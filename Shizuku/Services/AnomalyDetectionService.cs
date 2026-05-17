using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizuku.Hubs;
using Shizuku.Models;

namespace Shizuku.Services
{
    /// 異常支付偵測背景服務
    public class AnomalyDetectionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<AdminNotificationHub> _hubContext;
        private readonly ILogger<AnomalyDetectionService> _logger;

        // 已通報的異常紀錄（避免重複推播）
        private readonly HashSet<string> _notifiedAnomalies = new();

        public AnomalyDetectionService(
            IServiceScopeFactory scopeFactory,
            IHubContext<AdminNotificationHub> hubContext,
            ILogger<AnomalyDetectionService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("異常支付偵測服務已啟動...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DetectAnomaliesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "執行異常支付偵測時發生錯誤。");
                }

                // 每 60 秒執行一次掃描
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        /// 核心偵測邏輯：掃描近期交易紀錄，比對異常規則
        private async Task DetectAnomaliesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DbShizukuDemoContext>();

            var now = DateTime.Now;
            var tenMinutesAgo = now.AddMinutes(-10);

            // ====== 規則 1：高頻支付失敗 ======
            // 同一筆訂單在 10 分鐘內產生 >= 3 筆失敗紀錄
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
                _logger.LogWarning($"偵測到高頻支付失敗：訂單 ID {group.OrderId}，10 分鐘內失敗 {group.FailCount} 次");

                await _hubContext.Clients.Group("AdminNotifications").SendAsync(
                    "ReceiveAnomalyAlert",
                    "高頻支付失敗警報",
                    $"訂單 ID #{group.OrderId} 在 10 分鐘內產生了 {group.FailCount} 次支付失敗，疑似惡意測試或卡號異常。",
                    "warning"
                );
            }

            // ====== 規則 2：異常高額交易 ======
            // 單筆交易金額超過 $50,000
            var highAmountTxns = await db.TPaymentTransactions
                .Where(pt => pt.FAmount > 50000 && pt.FCreatedAt >= tenMinutesAgo)
                .ToListAsync();

            foreach (var txn in highAmountTxns)
            {
                var anomalyKey = $"high-amount-{txn.FId}";
                if (_notifiedAnomalies.Contains(anomalyKey)) continue;

                _notifiedAnomalies.Add(anomalyKey);
                _logger.LogWarning($"偵測到異常高額交易：交易 #{txn.FTransactionNo}，金額 ${txn.FAmount:N0}");

                await _hubContext.Clients.Group("AdminNotifications").SendAsync(
                    "ReceiveAnomalyAlert",
                    "異常高額交易警報",
                    $"交易單號 {txn.FTransactionNo} 金額達 ${txn.FAmount:N0}，已超過 $50,000 安全閾值，請立即確認。",
                    "danger"
                );
            }

            // 定期清理過舊的通報紀錄（防止記憶體持續增長）
            if (_notifiedAnomalies.Count > 500)
            {
                _notifiedAnomalies.Clear();
                _logger.LogInformation("已清理異常通報快取。");
            }
        }

        /// 手動觸發偵測（提供給 API Controller 呼叫，用於測試）
        public async Task TriggerManualDetection()
        {
            _logger.LogInformation("手動觸發異常支付偵測...");
            await DetectAnomaliesAsync();
        }

    }
}
