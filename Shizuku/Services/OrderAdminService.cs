using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shizuku.DTOs;
using Shizuku.Enums;
using Shizuku.Models;

namespace Shizuku.Services
{
    // 後台訂單行政管理服務
    // 專責處理後台人員對全站所有訂單的審查、明細調閱、狀態強制變更、批次出貨更新以及營收統計分析
    public class OrderAdminService
    {
        private readonly DbShizukuDemoContext _db;
        private readonly ProductService _productService;

        // 建構子注入
        public OrderAdminService(DbShizukuDemoContext db, ProductService productService)
        {
            _db = db;
            _productService = productService;
        }

        // 取得所有訂單資料
        public async Task<object> GetAllOrdersForAdminAsync()
        {
            var orderEntities = await _db.TOrders
                .OrderByDescending(o => o.FCreatedAt)
                .ToListAsync();

            return orderEntities.Select(o => new 
            {
                OrderNo = o.FOrderNo,
                MemberId = o.FMemberId,
                TotalAmount = o.FTotalAmount,
                CreatedAt = o.FCreatedAt,
                Status = o.FStatus, 
                StatusText = GetStatusText(o.FStatus)
            }).ToList();
        }

        // 取得單筆訂單明細
        public async Task<ApiResponse<OrderDetailDto>> GetOrderDetailForAdminAsync(string orderNo)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null)
            {
                return new ApiResponse<OrderDetailDto> { Success = false, Message = "找不到該筆訂單" };
            }

            var detailsData = await (from od in _db.TOrderDetails
                              join v in _db.TProductVariants on od.FVariantId equals v.FId
                              join p in _db.TProducts on v.FProductId equals p.FId
                              join c in _db.TProductColors on v.FColorId equals c.FId into cg
                              from color in cg.DefaultIfEmpty()
                              join s in _db.TProductSizes on v.FSizeId equals s.FId into sg
                              from size in sg.DefaultIfEmpty()
                              join img in _db.TProductImages.Where(i => i.FIsMain == 1) on p.FId equals img.FProductId into imgg
                              from mainImg in imgg.DefaultIfEmpty()
                              where od.FOrderId == order.FId
                              select new 
                              {
                                  Detail = od,
                                  ProductName = p.FName,
                                  ColorName = color != null ? color.FName : "",
                                  SizeName = size != null ? size.FName : "",
                                  ImageUrl = mainImg != null ? mainImg.FImageUrl : ""
                              }).ToListAsync();

            var paymentTransaction = await _db.TPaymentTransactions
                .Where(pt => pt.FOrderId == order.FId)
                .OrderByDescending(pt => pt.FCreatedAt)
                .FirstOrDefaultAsync();
            
            string paymentMethodName = "尚未指定";
            if (paymentTransaction != null)
            {
                paymentMethodName = await GetPaymentMethodNameAsync(paymentTransaction.FMethodId);
            }

            var dto = new OrderDetailDto
            {
                OrderNo = order.FOrderNo,
                CreatedAt = order.FCreatedAt,
                StatusText = GetStatusText(order.FStatus),
                TotalAmount = order.FTotalAmount,
                ReceiverName = order.FReceiverName,
                ReceiverPhone = order.FReceiverPhone,
                ReceiverAddress = order.FReceiverAddress,
                Note = order.FNote,
                PaymentMethod = paymentMethodName, 
                Subtotal = detailsData.Sum(d => d.Detail.FSubtotal),
                Discount = 0,
                ShippingFee = order.FTotalAmount - detailsData.Sum(d => d.Detail.FSubtotal),
                Items = detailsData.Select(d => new OrderItemDto
                {
                    ProductName = d.ProductName,
                    VariantName = (d.ColorName + " " + d.SizeName).Trim(),
                    UnitPrice = d.Detail.FPriceSnap,
                    Quantity = d.Detail.FQuantity,
                    ProductImage = d.ImageUrl
                }).ToList()
            };

            return new ApiResponse<OrderDetailDto> { Success = true, Message = "讀取成功", Data = dto };
        }
        
        // 強制更新訂單狀態
        public async Task<ApiResponse<object>> UpdateOrderStatusAsync(string orderNo, int newStatus)
        {
            if (newStatus == 5)
            {
                return await CancelOrderForAdminAsync(orderNo);
            }

            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null)
            {
                return new ApiResponse<object> { Success = false, Message = "找不到該筆訂單" };
            }

            order.FStatus = newStatus;
            order.FUpdatedAt = DateTime.Now;
            
            await _db.SaveChangesAsync();
            return new ApiResponse<object> { Success = true, Message = "訂單狀態更新成功" };
        }

        // 強制取消訂單並回補庫存
        public async Task<ApiResponse<object>> CancelOrderForAdminAsync(string orderNo)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null)
            {
                return new ApiResponse<object> { Success = false, Message = "找不到該筆訂單" };
            }
            if (order.FStatus == 5) 
            {
                return new ApiResponse<object> { Success = false, Message = "訂單已經是取消狀態" };
            }

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    order.FStatus = 5;
                    order.FUpdatedAt = DateTime.Now;

                    var details = await _db.TOrderDetails.Where(d => d.FOrderId == order.FId).ToListAsync();
                    foreach (var item in details)
                    {
                        bool isRestored = await _productService.RestoreStockAsync(item.FVariantId, item.FQuantity);
                        if (!isRestored)
                        {
                            throw new Exception($"回補庫存失敗，找不到規格 ID: {item.FVariantId}");
                        }
                    }

                    await _db.SaveChangesAsync();
                    transaction.Commit();
                    return new ApiResponse<object> { Success = true, Message = "後台強制取消訂單成功，庫存已回補" };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ApiResponse<object> { Success = false, Message = "強制取消訂單失敗：" + ex.Message };
                }
            }
        }

        // 取得待出貨或出貨中的訂單列表
        public async Task<List<object>> GetShippingOrdersAsync(int status)
        {
            var rawOrders = await _db.TOrders
                .Where(o => o.FStatus == status)
                .OrderBy(o => o.FCreatedAt)
                .Select(o => new
                {
                    o.FId,
                    o.FOrderNo,
                    o.FReceiverName,
                    o.FReceiverPhone,
                    o.FReceiverAddress,
                    o.FTotalAmount,
                    o.FCreatedAt
                })
                .ToListAsync();

            var shippingOrders = new List<object>();
            foreach (var o in rawOrders)
            {
                var details = await _db.TOrderDetails
                    .Where(od => od.FOrderId == o.FId)
                    .Select(od => $"{od.FProductNameSnap} x{od.FQuantity}")
                    .ToListAsync();

                shippingOrders.Add(new
                {
                    o.FId,
                    o.FOrderNo,
                    o.FReceiverName,
                    o.FReceiverPhone,
                    o.FReceiverAddress,
                    o.FTotalAmount,
                    o.FCreatedAt,
                    ItemSummary = string.Join(", ", details)
                });
            }

            return shippingOrders;
        }

        // 批次更新訂單狀態
        public async Task<ApiResponse<object>> BatchUpdateOrderStatusAsync(List<string> orderNos, int newStatus)
        {
            if (orderNos == null || !orderNos.Any())
            {
                return new ApiResponse<object> { Success = false, Message = "請選擇至少一筆訂單" };
            }

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    var orders = await _db.TOrders.Where(o => orderNos.Contains(o.FOrderNo)).ToListAsync();
                    foreach (var order in orders)
                    {
                        order.FStatus = newStatus;
                        order.FUpdatedAt = DateTime.Now;
                    }

                    await _db.SaveChangesAsync();
                    transaction.Commit();

                    return new ApiResponse<object> 
                    { 
                        Success = true, 
                        Message = $"成功批次更新 {orders.Count} 筆訂單為 {GetStatusText(newStatus)}" 
                    };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ApiResponse<object> { Success = false, Message = "批次更新失敗: " + ex.Message };
                }
            }
        }

        // 取得營收統計數據
        public async Task<object> GetRevenueStatsAsync(DateTime? startDate, DateTime? endDate)
        {
            var start = (startDate ?? DateTime.Today.AddDays(-6)).ToLocalTime();
            var end = (endDate ?? DateTime.Now).ToLocalTime();

            var validOrders = await _db.TOrders
                .Where(o => (o.FStatus == 2 || o.FStatus == 3 || o.FStatus == 4) && o.FCreatedAt >= start && o.FCreatedAt <= end)
                .ToListAsync();

            var totalGMV = validOrders.Sum(o => o.FTotalAmount);
            var totalOrders = validOrders.Count;
            var aov = totalOrders > 0 ? totalGMV / totalOrders : 0;

            var rangeDays = (end - start).TotalDays;
            object dailyStats;

            if (rangeDays > 60)
            {
                dailyStats = validOrders
                    .GroupBy(o => new { o.FCreatedAt.Year, o.FCreatedAt.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new
                    {
                        Date = $"{g.Key.Year}/{g.Key.Month:D2}",
                        Amount = g.Sum(o => o.FTotalAmount),
                        Count = g.Count()
                    })
                    .ToList();
            }
            else
            {
                dailyStats = validOrders
                    .GroupBy(o => o.FCreatedAt.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("MM/dd"),
                        Amount = g.Sum(o => o.FTotalAmount),
                        Count = g.Count()
                    })
                    .ToList();
            }

            var rawPaymentStats = await (from o in _db.TOrders
                                         join pt in _db.TPaymentTransactions on o.FId equals pt.FOrderId
                                         where (o.FStatus == 2 || o.FStatus == 3 || o.FStatus == 4) && o.FCreatedAt >= start && o.FCreatedAt <= end
                                         group o by pt.FMethodId into g
                                         select new
                                         {
                                             MethodId = g.Key,
                                             Amount = g.Sum(o => o.FTotalAmount)
                                         }).ToListAsync();

            var paymentMethods = await _db.TPaymentMethods
                .Where(pm => pm.FMethodName != null)
                .ToDictionaryAsync(pm => pm.FId, pm => pm.FMethodName!);

            var paymentStats = rawPaymentStats.Select(p => new
            {
                Method = (paymentMethods.TryGetValue(p.MethodId, out var name) && !string.IsNullOrEmpty(name))
                    ? name
                    : p.MethodId switch
                    {
                        1 => "綠界金流",
                        2 => "LINE Pay",
                        3 => "貨到付款",
                        _ => "未知"
                    },
                Amount = p.Amount
            }).ToList();

            return new
            {
                TotalGMV = totalGMV,
                TotalOrders = totalOrders,
                AOV = aov,
                DailyStats = dailyStats,
                PaymentStats = paymentStats
            };
        }

        // 解析訂單狀態中文語譯 (使用 strongly-typed OrderStatus 列舉)
        private string GetStatusText(int? status)
        {
            if (status == null) return "未知狀態";

            return (OrderStatus)status switch
            {
                OrderStatus.Pending => "未付款",
                OrderStatus.Paid => "已付款",
                OrderStatus.Shipping => "出貨中",
                OrderStatus.Delivered => "已送達",
                OrderStatus.Cancelled => "已取消",
                OrderStatus.PendingRefund => "待退款",
                OrderStatus.Refunded => "已退款",
                _ => "未知狀態"
            };
        }

        // 取得付款管道中文名稱
        private async Task<string> GetPaymentMethodNameAsync(int? methodId)
        {
            if (methodId == null) return "未知付款方式";
            var method = await _db.TPaymentMethods.FirstOrDefaultAsync(m => m.FId == methodId);
            if (method != null) return method.FMethodName;

            return methodId switch
            {
                1 => "綠界金流",
                2 => "LINE Pay",
                3 => "貨到付款",
                _ => "未知付款方式"
            };
        }
    }
}
