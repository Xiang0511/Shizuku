using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizuku.Hubs;
using Shizuku.Models;

namespace Shizuku.Services
{
    // 金流安全異常偵測背景服務
    // 定時掃描支付交易資料，偵測金流層面的可疑行為並推播警報
    // 透過 IHubContext 注入推播，不直接依賴 Hub 實例
    // 推播時帶上 category = "payment"，確保只有金流控制中心接收
    public class PaymentAnomalyService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<AdminNotificationHub> _hubContext;
        private readonly ILogger<PaymentAnomalyService> _logger;

        // 已通報的異常紀錄，避免重複推播（Key 格式：規則類型-識別碼-小時）
        private readonly HashSet<string> _notifiedAnomalies = new();

        public PaymentAnomalyService(
            IServiceScopeFactory scopeFactory,
            IHubContext<AdminNotificationHub> hubContext,
            ILogger<PaymentAnomalyService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("金流安全異常偵測服務已啟動...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ScanAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "執行金流異常偵測時發生錯誤。");
                }

                // 每 60 秒掃描一次
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        // 核心掃描邏輯（可被手動觸發 API 呼叫）
        public async Task ScanAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DbShizukuDemoContext>();

            var now = DateTime.Now;
            var tenMinutesAgo = now.AddMinutes(-10);

            // ====== 規則 1：高頻支付失敗 ======
            // 同一筆訂單在 10 分鐘內產生 >= 3 筆失敗紀錄，疑似惡意刷卡測試
            var failedGroups = await db.TPaymentTransactions
                .Where(pt => pt.FStatus == 0 && pt.FCreatedAt >= tenMinutesAgo)
                .GroupBy(pt => pt.FOrderId)
                .Select(g => new { OrderId = g.Key, FailCount = g.Count() })
                .Where(g => g.FailCount >= 3)
                .ToListAsync();

            foreach (var group in failedGroups)
            {
                var anomalyKey = $"high-freq-{group.OrderId}-{now:yyyyMMddHH}";
                if (_notifiedAnomalies.Contains(anomalyKey)) continue;

                _notifiedAnomalies.Add(anomalyKey);
                _logger.LogWarning("偵測到高頻支付失敗：訂單 ID {OrderId}，10 分鐘內失敗 {FailCount} 次",
                    group.OrderId, group.FailCount);

                await _hubContext.Clients.Group(AdminNotificationHub.GroupName).SendAsync(
                    "ReceiveAnomalyAlert",
                    "高頻支付失敗警報",
                    $"訂單 ID #{group.OrderId} 在 10 分鐘內產生了 {group.FailCount} 次支付失敗，疑似惡意刷卡測試或卡號異常，請立即查閱。",
                    "warning",
                    "payment"  // category：僅推播至金流控制中心
                );
            }

            // ====== 規則 2：異常高額交易 ======
            // 單筆交易金額超過 $50,000，可能存在金額竄改或洗錢風險
            var highAmountTxns = await db.TPaymentTransactions
                .Where(pt => pt.FAmount > 50000 && pt.FCreatedAt >= tenMinutesAgo)
                .ToListAsync();

            foreach (var txn in highAmountTxns)
            {
                var anomalyKey = $"high-amount-{txn.FId}";
                if (_notifiedAnomalies.Contains(anomalyKey)) continue;

                _notifiedAnomalies.Add(anomalyKey);
                _logger.LogWarning("偵測到異常高額交易：交易 #{TransactionNo}，金額 ${Amount:N0}",
                    txn.FTransactionNo, txn.FAmount);

                await _hubContext.Clients.Group(AdminNotificationHub.GroupName).SendAsync(
                    "ReceiveAnomalyAlert",
                    "異常高額交易警報",
                    $"交易單號 {txn.FTransactionNo} 金額達 ${txn.FAmount:N0}，已超過 $50,000 安全閾值，請立即確認是否為正常授權交易。",
                    "danger",
                    "payment"  // category：僅推播至金流控制中心
                );
            }

            // 定期清理過舊的通報紀錄，防止記憶體持續增長
            if (_notifiedAnomalies.Count > 500)
            {
                _notifiedAnomalies.Clear();
                _logger.LogInformation("已清理金流異常通報快取。");
            }
        }
    }
}
