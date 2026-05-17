using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizuku.Hubs;
using Shizuku.Models;

namespace Shizuku.Services
{
    /// 訂單行為異常偵測背景服務
    /// 職責：定時掃描訂單與會員行為，偵測訂單層面的業務異常並推播警報
    /// 解耦：透過 IHubContext 注入推播，不直接依賴 Hub 實例
    /// 分類：推播時帶上 category = "order"，確保只有訂單控制中心接收
    public class OrderAnomalyService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<AdminNotificationHub> _hubContext;
        private readonly ILogger<OrderAnomalyService> _logger;

        // 已通報的異常紀錄，避免重複推播
        private readonly HashSet<string> _notifiedAnomalies = new();

        public OrderAnomalyService(
            IServiceScopeFactory scopeFactory,
            IHubContext<AdminNotificationHub> hubContext,
            ILogger<OrderAnomalyService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("訂單行為異常偵測服務已啟動...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ScanAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "執行訂單異常偵測時發生錯誤。");
                }

                // 每 120 秒掃描一次（訂單行為異常比金流安全問題嚴重性稍低，掃描間隔可稍長）
                await Task.Delay(TimeSpan.FromSeconds(120), stoppingToken);
            }
        }

        /// 核心掃描邏輯（可被手動觸發 API 呼叫）   
        public async Task ScanAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DbShizukuDemoContext>();

            var now = DateTime.Now;
            var yesterday = now.AddDays(-1);

            // ====== 規則 1：Conflict 金流衝突 ======
            // 訂單狀態為已取消(5)，但金流交易紀錄卻顯示成功(1)
            // 這通常發生在系統超時自動取消，但金流已經在外部扣款成功的情境
            var conflictOrders = await (from o in db.TOrders
                                        join pt in db.TPaymentTransactions on o.FId equals pt.FOrderId
                                        join m in db.TMembers on o.FMemberId equals m.FId
                                        where o.FStatus == 5 && pt.FStatus == 1
                                        select new { o.FOrderNo, MemberName = m.FName, o.FTotalAmount })
                                       .ToListAsync();

            foreach (var conflict in conflictOrders)
            {
                var anomalyKey = $"conflict-{conflict.FOrderNo}";
                if (_notifiedAnomalies.Contains(anomalyKey)) continue;

                _notifiedAnomalies.Add(anomalyKey);
                _logger.LogWarning("偵測到金流衝突：訂單 {OrderNo} 已取消但金流成功", conflict.FOrderNo);

                await _hubContext.Clients.Group(AdminNotificationHub.GroupName).SendAsync(
                    "ReceiveAnomalyAlert",
                    "金流衝突警報",
                    $"訂單 {conflict.FOrderNo}（{conflict.MemberName}，金額 ${conflict.FTotalAmount:N0}）已被系統取消，但金流端回傳付款成功。請立即執行「訂單救援」以恢復訂單並扣除庫存。",
                    "danger",
                    "order"  // category：僅推播至訂單控制中心
                );
            }

            // ====== 規則 2：Behavior 惡意鎖單 ======
            // 同一會員在 24 小時內取消訂單 >= 3 筆，疑似惡意占用庫存
            var suspiciousMembers = await db.TOrders
                .Where(o => o.FStatus == 5 && o.FCreatedAt > yesterday)
                .GroupBy(o => o.FMemberId)
                .Where(g => g.Count() >= 3)
                .Select(g => new { MemberId = g.Key, CancelCount = g.Count() })
                .ToListAsync();

            foreach (var suspect in suspiciousMembers)
            {
                var anomalyKey = $"behavior-{suspect.MemberId}-{now:yyyyMMdd}";
                if (_notifiedAnomalies.Contains(anomalyKey)) continue;

                _notifiedAnomalies.Add(anomalyKey);

                // 查詢會員姓名
                var member = await db.TMembers
                    .Where(m => m.FId == suspect.MemberId)
                    .Select(m => new { m.FName, m.FEmail })
                    .FirstOrDefaultAsync();

                _logger.LogWarning("偵測到惡意鎖單行為：會員 ID {MemberId}，24 小時內取消 {Count} 筆",
                    suspect.MemberId, suspect.CancelCount);

                await _hubContext.Clients.Group(AdminNotificationHub.GroupName).SendAsync(
                    "ReceiveAnomalyAlert",
                    "惡意鎖單行為警報",
                    $"會員「{member?.FName ?? "未知"}」（{member?.FEmail}）在 24 小時內已取消 {suspect.CancelCount} 筆訂單，疑似惡意占用庫存。建議立即查閱該會員歷史記錄並考慮停權。",
                    "warning",
                    "order"  // category：僅推播至訂單控制中心
                );
            }

            // 定期清理過舊的通報紀錄
            if (_notifiedAnomalies.Count > 500)
            {
                _notifiedAnomalies.Clear();
                _logger.LogInformation("已清理訂單異常通報快取。");
            }
        }
    }
}
