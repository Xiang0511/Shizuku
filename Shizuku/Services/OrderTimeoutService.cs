using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shizuku.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizuku.Services
{
    public class OrderTimeoutService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderTimeoutService> _logger;

        public OrderTimeoutService(IServiceScopeFactory scopeFactory, ILogger<OrderTimeoutService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("訂單超時檢查服務已啟動...");

            // 只要背景任務沒被停止，就一直執行
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<DbShizukuDemoContext>();
                        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

                        // 1. 找出「待付款 (1)」且「建立時間超過 10 分鐘」的訂單
                        var timeoutThreshold = DateTime.Now.AddMinutes(-10);
                        var timeoutOrders = db.TOrders
                            .Where(o => o.FStatus == 1 && o.FCreatedAt < timeoutThreshold)
                            .ToList();

                        if (timeoutOrders.Any())
                        {
                            _logger.LogInformation($"發現 {timeoutOrders.Count} 筆超時未付款訂單，準備自動取消...");

                            foreach (var order in timeoutOrders)
                            {
                                // 2. 呼叫 CancelOrderAsync 方法 (傳入唯一 ID 以避免重複單號衝突)
                                var result = await orderService.CancelOrderAsync(order.FId);
                                if (result.Success)
                                {
                                    _logger.LogInformation($"訂單 {order.FOrderNo} (ID: {order.FId}) 已因超時自動取消。");
                                }
                                else
                                {
                                    _logger.LogError($"自動取消訂單 {order.FOrderNo} (ID: {order.FId}) 失敗：{result.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "執行訂單超時檢查時發生錯誤。");
                }

                // 每 1 分鐘檢查一次
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
