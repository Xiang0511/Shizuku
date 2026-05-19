using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shizuku.DTOs;
using Shizuku.Models;

namespace Shizuku.Services
{
    // 全站異常安全監控與訂單救援服務
    // 專責掃描偵測金流狀態衝突、同一會員惡意鎖單、高頻支付失敗、高額交易警示，並提供異常訂單的安全回扣庫存救援機制
    public class AnomalyMonitorService
    {
        private readonly DbShizukuDemoContext _db;
        private readonly ProductService _productService;

        // 建構子注入
        public AnomalyMonitorService(DbShizukuDemoContext db, ProductService productService)
        {
            _db = db;
            _productService = productService;
        }

        // 查詢特定時間段內高頻支付失敗的訂單
        public async Task<List<object>> GetHighFreqFailuresAsync(DateTime since)
        {
            var rawData = await _db.TPaymentTransactions
                .Where(pt => pt.FStatus == 0 && pt.FCreatedAt >= since)
                .GroupBy(pt => pt.FOrderId)
                .Select(g => new
                {
                    OrderId = g.Key,
                    FailCount = g.Count(),
                    LatestTime = g.Max(pt => pt.FCreatedAt)
                })
                .Where(g => g.FailCount >= 3)
                .OrderByDescending(g => g.FailCount)
                .ToListAsync();

            return rawData.Select(r => new
            {
                OrderId = r.OrderId,
                FailCount = r.FailCount,
                LatestTime = r.LatestTime.ToString("MM/dd HH:mm:ss")
            }).Cast<object>().ToList();
        }

        // 查詢特定時間段內金額異常高額的交易
        public async Task<List<object>> GetHighAmountTxnsAsync(DateTime since)
        {
            var rawData = await _db.TPaymentTransactions
                .Where(pt => pt.FAmount > 50000 && pt.FCreatedAt >= since)
                .Select(pt => new
                {
                    TransactionNo = pt.FTransactionNo,
                    Amount = pt.FAmount,
                    CreatedAt = pt.FCreatedAt
                })
                .OrderByDescending(pt => pt.CreatedAt)
                .ToListAsync();

            return rawData.Select(r => new
            {
                TransactionNo = r.TransactionNo,
                Amount = r.Amount,
                CreatedAt = r.CreatedAt.ToString("MM/dd HH:mm:ss")
            }).Cast<object>().ToList();
        }

        // 掃描全站潛在的異常訂單
        public async Task<List<AbnormalOrderDto>> GetAbnormalOrdersAsync()
        {
            var abnormalOrders = new List<AbnormalOrderDto>();

            // 偵測金流衝突：訂單已取消(5)，但金流紀錄卻有成功(1)
            var conflictData = await (from o in _db.TOrders
                                     join pt in _db.TPaymentTransactions on o.FId equals pt.FOrderId
                                     join m in _db.TMembers on o.FMemberId equals m.FId
                                     where o.FStatus == 5 && pt.FStatus == 1
                                     select new { o, MemberName = m.FName }).ToListAsync();

            abnormalOrders.AddRange(conflictData.Select(x => new AbnormalOrderDto
            {
                OrderNo = x.o.FOrderNo,
                MemberName = x.MemberName,
                TotalAmount = x.o.FTotalAmount,
                Status = x.o.FStatus,
                StatusText = "已取消",
                CreatedAt = x.o.FCreatedAt,
                AbnormalityType = "Conflict",
                Description = "訂單已被系統取消，但金流端回傳付款成功。",
                Suggestion = "請執行「強制救援」恢復此訂單並扣除庫存。"
            }));

            // 偵測惡意鎖單行為：同一會員 24 小時內取消超過 3 次
            var yesterday = DateTime.Now.AddDays(-1);
            var badUsers = await _db.TOrders
                .Where(o => o.FStatus == 5 && o.FCreatedAt > yesterday)
                .GroupBy(o => o.FMemberId)
                .Where(g => g.Count() >= 3)
                .Select(g => g.Key)
                .ToListAsync();

            if (badUsers.Any())
            {
                var badOrderData = await (from o in _db.TOrders
                                          join m in _db.TMembers on o.FMemberId equals m.FId
                                          where badUsers.Contains(o.FMemberId) && o.FStatus == 5 && o.FCreatedAt > yesterday
                                          orderby o.FCreatedAt descending
                                          select new 
                                          {
                                              Order = o,
                                              MemberName = m.FName
                                          })
                                         .ToListAsync();

                var behaviorAlerts = badOrderData
                    .GroupBy(x => x.Order.FMemberId)
                    .Select(g => {
                        var latest = g.First();
                        return new AbnormalOrderDto
                        {
                            OrderNo = latest.Order.FOrderNo,
                            MemberName = latest.MemberName,
                            TotalAmount = latest.Order.FTotalAmount,
                            Status = latest.Order.FStatus,
                            StatusText = "已取消",
                            CreatedAt = latest.Order.FCreatedAt,
                            AbnormalityType = "Behavior",
                            Description = $"此會員在 24 小時內有 {g.Count()} 筆取消紀錄，疑似惡意占用庫存。",
                            RelatedCount = g.Count(),
                            Suggestion = "建議檢視該會員歷史紀錄，必要時予以停權。"
                        };
                    }).ToList();
                
                abnormalOrders.AddRange(behaviorAlerts);
            }

            return abnormalOrders.OrderByDescending(a => a.CreatedAt).ToList();
        }

        // 強制救援誤殺訂單：將已取消(5)恢復為已付款(2)，並重新扣除庫存
        public async Task<ApiResponse<object>> RescueOrderAsync(string orderNo)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null) return new ApiResponse<object> { Success = false, Message = "找不到該筆訂單" };
            if (order.FStatus != 5) return new ApiResponse<object> { Success = false, Message = "此訂單並非取消狀態，不需救援" };

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    // 重新檢查並扣除庫存
                    var details = await _db.TOrderDetails.Where(d => d.FOrderId == order.FId).ToListAsync();
                    foreach (var item in details)
                    {
                        bool stockDeducted = await _productService.DeductStockAsync(item.FVariantId, item.FQuantity);
                        if (!stockDeducted)
                        {
                            throw new Exception($"商品規格 ID {item.FVariantId} 庫存不足，無法恢復訂單！");
                        }
                    }

                    // 更新訂單狀態為已付款
                    order.FStatus = 2;
                    order.FUpdatedAt = DateTime.Now;

                    // 確保金流交易狀態也是成功
                    var payment = await _db.TPaymentTransactions
                        .Where(pt => pt.FOrderId == order.FId)
                        .OrderByDescending(pt => pt.FCreatedAt)
                        .FirstOrDefaultAsync();

                    if (payment != null)
                    {
                        payment.FStatus = 1;
                        if (payment.FPaidAt == null) payment.FPaidAt = DateTime.Now;
                    }

                    await _db.SaveChangesAsync();
                    transaction.Commit();

                    return new ApiResponse<object> { Success = true, Message = $"訂單 {orderNo} 已成功恢復為已付款狀態，庫存已重新扣除。" };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ApiResponse<object> { Success = false, Message = "救援失敗：" + ex.Message };
                }
            }
        }
    }
}
