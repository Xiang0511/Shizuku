using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shizuku.DTOs;
using Shizuku.Models;

namespace Shizuku.Services
{
    // 前台會員訂單交易核心服務
    // 專責處理前台會員購物車結帳、訂單創立、庫存扣減、會員訂單明細查詢、自行取消以及綠界/LINE Pay 付款連結之動態生成
    public class OrderService
    {
        private readonly DbShizukuDemoContext _db;
        private readonly ProductService _productService;
        private readonly PaymentFactory _paymentFactory;

        // 建構子注入
        public OrderService(
            DbShizukuDemoContext db, 
            ProductService productService, 
            PaymentFactory paymentFactory)
        {
            _db = db;
            _productService = productService;
            _paymentFactory = paymentFactory;
        }

        // 前台會員結帳建立新訂單
        public async Task<ApiResponse<CreateOrderResponseDto>> CreateOrder(CreateOrderRequestDto request)
        {
            if (request.CartItems == null || !request.CartItems.Any())
            {
                return new ApiResponse<CreateOrderResponseDto> { Success = false, Message = "購物車內無商品，無法建立訂單！" };
            }

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    // 產生不重複訂單編號
                    string orderNo = GenerateOrderNo();

                    // 計算商品總價並即時扣庫存
                    decimal subtotal = 0;
                    var orderDetails = new List<TOrderDetail>();

                    foreach (var item in request.CartItems)
                    {
                        var variant = await _db.TProductVariants
                            .Include(v => v.TProduct)
                            .FirstOrDefaultAsync(v => v.FId == item.VariantId);

                        if (variant == null)
                        {
                            throw new Exception($"找不到商品規格 ID: {item.VariantId}");
                        }

                        // 即時扣庫存 (原子操作)
                        bool stockDeducted = await _productService.DeductStockAsync(item.VariantId, item.Quantity);
                        if (!stockDeducted)
                        {
                            throw new Exception($"商品「{variant.TProduct?.FName}」庫存不足，無法建立訂單！");
                        }

                        decimal price = variant.TProduct?.FPrice ?? 0;
                        decimal itemTotal = price * item.Quantity;
                        subtotal += itemTotal;

                        orderDetails.Add(new TOrderDetail
                        {
                            FVariantId = item.VariantId,
                            FProductNameSnap = variant.TProduct?.FName ?? "未知商品",
                            FPriceSnap = price,
                            FQuantity = item.Quantity,
                            FSubtotal = itemTotal
                        });
                    }

                    // 運費計算邏輯：滿 1500 免運，否則運費 80 元
                    decimal shippingFee = subtotal >= 1500 ? 0 : 80;
                    decimal totalAmount = subtotal + shippingFee;

                    // 寫入訂單主表
                    var order = new TOrder
                    {
                        FOrderNo = orderNo,
                        FMemberId = request.MemberId,
                        FTotalAmount = totalAmount,
                        FReceiverName = request.ReceiverName,
                        FReceiverPhone = request.ReceiverPhone,
                        FReceiverAddress = request.ReceiverAddress,
                        FNote = request.Note,
                        FStatus = 1, // 預設狀態為「未付款」
                        FCreatedAt = DateTime.Now,
                        FUpdatedAt = DateTime.Now
                    };

                    _db.TOrders.Add(order);
                    await _db.SaveChangesAsync();

                    // 寫入訂單明細表
                    foreach (var detail in orderDetails)
                    {
                        detail.FOrderId = order.FId;
                        _db.TOrderDetails.Add(detail);
                    }
                    await _db.SaveChangesAsync();

                    // 寫入金流交易主表 (PaymentTransactions)
                    var paymentTx = new TPaymentTransaction
                    {
                        FOrderId = order.FId,
                        FTransactionNo = "TX" + orderNo.Substring(2),
                        FMethodId = request.PaymentMethodId,
                        FAmount = totalAmount,
                        FStatus = 0, // 交易預設為「未付款/待處理」
                        FCreatedAt = DateTime.Now
                    };
                    _db.TPaymentTransactions.Add(paymentTx);
                    await _db.SaveChangesAsync();

                    // 依照金流管道生成付款連結
                    string paymentUrl = "";

                    // 貨到付款 (ID=3) 直接將狀態標記為「已付款」，防止逾時被背景服務自動取消
                    if (request.PaymentMethodId == 3)
                    {
                        order.FStatus = 2; // 貨到付款默認視為「已付款」以不被自動超時取消
                        paymentTx.FStatus = 1; // 標記為成功
                        paymentTx.FPaidAt = DateTime.Now;
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        paymentUrl = await GeneratePaymentUrlAsync(orderNo, request.PaymentMethodId, totalAmount);
                    }

                    transaction.Commit();

                    return new ApiResponse<CreateOrderResponseDto>
                    {
                        Success = true,
                        Message = "訂單建立成功！",
                        Data = new CreateOrderResponseDto
                        {
                            OrderNo = orderNo,
                            PaymentUrl = paymentUrl
                        }
                    };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ApiResponse<CreateOrderResponseDto>
                    {
                        Success = false,
                        Message = "建立訂單失敗：" + ex.Message,
                        Data = null
                    };
                }
            }
        }

        // 取得會員前台的訂單列表
        public async Task<List<OrderListDto>> GetMemberOrdersAsync(int memberId)
        {
            var orders = await _db.TOrders
                .Where(o => o.FMemberId == memberId)
                .OrderByDescending(o => o.FCreatedAt)
                .ToListAsync();

            var list = new List<OrderListDto>();
            foreach (var o in orders)
            {
                list.Add(new OrderListDto
                {
                    OrderNo = o.FOrderNo,
                    CreatedAt = o.FCreatedAt,
                    StatusText = GetStatusText(o.FStatus),
                    TotalAmount = o.FTotalAmount
                });
            }

            return list;
        }

        // 取得會員前台的單筆訂單明細
        public async Task<ApiResponse<OrderDetailDto>> GetOrderDetailAsync(string orderNo, int memberId)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo && o.FMemberId == memberId);
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

        // 產生第三方金流支付跳轉連結
        public async Task<string> GeneratePaymentUrlAsync(string orderNo, int paymentMethodId, decimal totalAmount)
        {
            var paymentService = _paymentFactory.GetPaymentService(paymentMethodId);
            return await paymentService.GeneratePaymentUrlAsync(orderNo, totalAmount);
        }

        // 產生綠界自動 Submit 的 HTML Form 字串
        public async Task<string> GenerateECPayHtmlFormAsync(string orderNo)
        {
            var ecpayService = _paymentFactory.GetPaymentService(1) ;
            if (ecpayService == null) return "";

            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null) return "";

            return await ecpayService.GenerateHtmlFormAsync(orderNo);
        }

        // 取得前台統計用銷量數據
        public async Task<List<VariantSalesStatsDto>> GetSalesStatsAsync()
        {
            var stats = await (from od in _db.TOrderDetails
                               join o in _db.TOrders on od.FOrderId equals o.FId
                               where o.FStatus != 5 // 排除取消的訂單
                               group od by od.FVariantId into g
                               select new VariantSalesStatsDto
                               {
                                   VariantId = g.Key,
                                   TotalQuantitySold = g.Sum(x => x.FQuantity),
                                   TotalRevenue = g.Sum(x => x.FSubtotal)
                               })
                               .OrderByDescending(x => x.TotalQuantitySold)
                               .Take(10)
                               .ToListAsync();

            return stats;
        }

        // 會員自行取消訂單並回補庫存
        public async Task<ApiResponse<object>> CancelOrderAsync(string orderNo)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null)
            {
                return new ApiResponse<object> { Success = false, Message = "找不到該筆訂單" };
            }
            if (order.FStatus != 1) 
            {
                return new ApiResponse<object> { Success = false, Message = "只有未付款訂單才能取消" };
            }

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    order.FStatus = 5; // 狀態變更為「已取消」
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
                    return new ApiResponse<object> { Success = true, Message = "訂單取消成功，庫存已回補" };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ApiResponse<object> { Success = false, Message = "取消訂單失敗：" + ex.Message };
                }
            }
        }

        // 取得單筆資料庫訂單實體
        public async Task<TOrder> GetOrderAsync(string orderNo)
        {
            return await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
        }

        // 統一更新訂單狀態為已付款
        public async Task<bool> MarkOrderAsPaidAsync(string orderNo, int? paymentMethodId = null)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null) return false;

            order.FStatus = 2; // 已付款
            order.FUpdatedAt = DateTime.Now;

            var paymentTx = await _db.TPaymentTransactions
                .Where(pt => pt.FOrderId == order.FId)
                .OrderByDescending(pt => pt.FCreatedAt)
                .FirstOrDefaultAsync();

            if (paymentTx != null)
            {
                paymentTx.FStatus = 1; // 成功
                paymentTx.FPaidAt = DateTime.Now;
                if (paymentMethodId != null)
                {
                    paymentTx.FMethodId = paymentMethodId.Value;
                }
            }

            await _db.SaveChangesAsync();
            return true;
        }

        // 產生不重複之訂單編號
        private string GenerateOrderNo()
        {
            string dateStr = DateTime.Now.ToString("yyyyMMdd");
            string randomStr = new Random().Next(100, 999).ToString();
            return $"ORD{dateStr}{randomStr}";
        }

        // 解析訂單狀態中文語譯
        private string GetStatusText(int? status)
        {
            return status switch
            {
                1 => "未付款",
                2 => "已付款",
                3 => "出貨中",
                4 => "已送達",
                5 => "已取消",
                _ => "未知狀態"
            };
        }

        // 取得付款管道中文名稱
        private async Task<string> GetPaymentMethodNameAsync(int? methodId)
        {
            if (methodId == null) return "未知付款方式";
            var method = await _db.TPaymentMethods.FirstOrDefaultAsync(m => m.FId == methodId);
            return method != null ? method.FMethodName : "未知付款方式";
        }
    }
}
