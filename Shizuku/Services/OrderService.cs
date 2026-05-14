using Microsoft.EntityFrameworkCore;
using Shizuku.DTOs;
using Shizuku.Models;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using System.Web;


namespace Shizuku.Services
{
    public class OrderService
    {
        // 宣告一個唯讀的私有變數，用來存放資料庫連線
        private readonly DbShizukuDemoContext _db;
        private readonly LinePayService _linePayService;
        private readonly ProductService _productService;
        private readonly IConfiguration _config; //讀取 appsettings.json裡的設定
        private readonly PaymentFactory _paymentFactory;

        // 建構子 (Constructor)
        // 當 DI 容器要建立 OrderService 時，發現它需要 DbShizukuDemoContext 和 LinePayService，就會自動塞進來
    public OrderService(DbShizukuDemoContext db, ProductService productService, PaymentFactory paymentFactory)
    {
        _db = db;
        _productService = productService;
        _paymentFactory = paymentFactory;
    }

        //建立訂單
        public async Task<ApiResponse<CreateOrderResponseDto>> CreateOrder(CreateOrderRequestDto request)
        {
            // 1. 先檢查購物車是不是空的
            if (request.CartItems == null || request.CartItems.Count == 0)
                return new ApiResponse<CreateOrderResponseDto> { Success = false, Message = "購物車是空的喔！" };

            // 2. 產生一個唯一的訂單編號 (例如: ORD20260502123456)
            string newOrderNo = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss");
            decimal totalAmount = 0; // 用來累加總金額

            // 3. 開啟資料庫交易 (Transaction) 防護罩！
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    // 準備一個空箱子，用來裝所有的訂單明細
                    List<TOrderDetail> details = new List<TOrderDetail>();

                    // 步驟 4：跑一個 foreach 迴圈，檢查購物車裡的每一個商品
                    foreach (var item in request.CartItems)
                    {
                        // 4-1: 去 _db.TProductVariants 找出這個商品規格
                        var variant = _db.TProductVariants.FirstOrDefault(v => v.FId == item.VariantId);
                        if (variant == null)
                        {
                            throw new Exception($"找不到商品規格代碼 {item.VariantId}");
                        }
                        //減去庫存
                        bool stockDeducted = await _productService.DeductStockAsync(item.VariantId, item.Quantity);
                        if (!stockDeducted)
                        {
                            throw new Exception($"商品規格 {item.VariantId} 庫存不足，下單失敗！");
                        }

                        // 4-4: 為了拿到商品的名字，我們去 TProduct 找一下主商品資料
                        var product = _db.TProducts.FirstOrDefault(p => p.FId == variant.FProductId);
                        string productName = product != null ? product.FName : "未知商品";

                        // 決定結帳價格 (如果 Variant 沒特別設定價格，就用 Product 的預設價格)
                        decimal unitPrice = variant.FPrice ?? (product?.FPrice ?? 0);
                        decimal subtotal = unitPrice * item.Quantity; // 算出小計

                        // 4-5: 建立一個 TOrderDetail (明細) 並且放入剛剛的箱子裡
                        details.Add(new TOrderDetail
                        {
                            FVariantId = item.VariantId,
                            FProductNameSnap = productName,
                            FPriceSnap = unitPrice,
                            FQuantity = item.Quantity,
                            FSubtotal = subtotal
                        });

                        // 4-6: 把小計金額累加到這整張訂單的總金額上
                        totalAmount += subtotal;
                    }

                    // 步驟 5：建立一張主訂單 TOrder
                    var newOrder = new TOrder
                    {
                        FOrderNo = newOrderNo,
                        FMemberId = request.MemberId,
                        FTotalAmount = totalAmount,
                        FStatus = 1, // 1: 代表待處理 (待付款)
                        FReceiverName = request.ReceiverName,
                        FReceiverPhone = request.ReceiverPhone,
                        FReceiverAddress = request.ReceiverAddress,
                        FNote = request.Note,
                        FCreatedAt = DateTime.Now,
                        FUpdatedAt = DateTime.Now
                    };

                    // 先把主訂單寫入資料庫，這樣系統才會發一個最新的 FId (訂單流水號) 給它
                    _db.TOrders.Add(newOrder);
                    _db.SaveChanges();

                    // 把剛拿到的新訂單流水號，塞給箱子裡面的每一個明細，然後存入資料庫
                    foreach (var detail in details)
                    {
                        detail.FOrderId = newOrder.FId;
                        _db.TOrderDetails.Add(detail);
                    }

                    _db.SaveChanges();

                    //建立金流交易主檔(TPaymentTransaction)
                    var paymentTransaction = new TPaymentTransaction
                    {
                        //生成一個唯一的支付單號
                        FTransactionNo = "PAY" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(100,999),
                        FOrderId = newOrder.FId, 
                        FMemberId = request.MemberId,
                        FMethodId = request.PaymentMethodId,
                        FAmount = totalAmount,
                        FStatus = 0, // 0: 待付款
                        FCreatedAt = DateTime.Now
                    };

                    _db.TPaymentTransactions.Add(paymentTransaction);
                    await _db.SaveChangesAsync();//儲存資料庫

                    string paymentUrl = await GeneratePaymentUrlAsync(newOrderNo, request.PaymentMethodId, totalAmount);
                    transaction.Commit();

                    // 無論是哪種付款方式，成功後統一在這裡回傳給前端
                    return new ApiResponse<CreateOrderResponseDto>
                    {
                        Success = true,
                        Message = "訂單建立成功！",
                        Data = new CreateOrderResponseDto
                        {
                            OrderNo = newOrderNo,
                            PaymentUrl = paymentUrl
                        }
                    };
                }
                catch (Exception ex)
                {
                    // 如果上面任何一個步驟發生錯誤 (例如庫存不足、資料庫當機)
                    // 就會跳到這裡，執行 Rollback (時光倒流)，什麼都不會存進去資料庫
                    transaction.Rollback();

                    return new ApiResponse<CreateOrderResponseDto> { Success = false, Message = "訂單建立失敗：" + ex.Message };
                }
            }
        }

        //根據memberId 取的訂單列表
        public async Task<List<OrderListDto>> GetMemberOrdersAsync(int memberId)
        {
            //先從資料庫撈出原始資料 (不要在 Select 裡轉狀態)
            var orderEntities = await _db.TOrders
            .Where(o => o.FMemberId == memberId)
            .OrderByDescending(o => o.FCreatedAt)
            .ToListAsync();

            var orders = orderEntities.Select(o => new OrderListDto
            {
                OrderNo = o.FOrderNo,
                TotalAmount = o.FTotalAmount,
                CreatedAt = o.FCreatedAt,
                StatusText = GetStatusText(o.FStatus)
            }).ToList();
            return orders;
        }


        //根據orderNo 取的訂單明細
        public async Task<ApiResponse<OrderDetailDto>> GetOrderDetailAsync(string orderNo, int memberId)
        {
            // 1. 先單獨把訂單主表撈出來
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo && o.FMemberId == memberId);
            
            if (order == null)
            {
                return new ApiResponse<OrderDetailDto> { Success = false, Message = "找不到該筆訂單" };
            }
            // 2. 透過原生的 LINQ Join 語法，把明細、規格、商品、顏色、尺寸、圖片全部安全地串聯起來！
            var detailsData = await (from od in _db.TOrderDetails
                             join v in _db.TProductVariants on od.FVariantId equals v.FId
                             join p in _db.TProducts on v.FProductId equals p.FId
                             // 顏色與尺寸可能為空，所以使用 Left Join
                             join c in _db.TProductColors on v.FColorId equals c.FId into cg
                             from color in cg.DefaultIfEmpty()
                             join s in _db.TProductSizes on v.FSizeId equals s.FId into sg
                             from size in sg.DefaultIfEmpty()
                             // 圖片獨立一張表，且只要抓「主圖」 (FIsMain == 1)
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

            // 找出付款方式
            var paymentTransaction = await _db.TPaymentTransactions
                .Where(pt => pt.FOrderId == order.FId)
                .OrderByDescending(pt => pt.FCreatedAt)
                .FirstOrDefaultAsync();
            
            string paymentMethodName = "尚未指定";
            if (paymentTransaction != null)
            {
                var method = await _db.TPaymentMethods.FirstOrDefaultAsync(m => m.FId == paymentTransaction.FMethodId);
                if (method != null)
                {
                    paymentMethodName = method.FMethodName;
                }
            }

            // 3. 把撈出來的安全資料轉換給前端 (DTO)
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
                ShippingFee = 0,
                Items = detailsData.Select(d => new OrderItemDto
                {
                    ProductName = d.ProductName,
                    // 將顏色與尺寸組合起來 (例如: "紅色 XL")
                    VariantName = (d.ColorName + " " + d.SizeName).Trim(),
                    UnitPrice = d.Detail.FPriceSnap,
                    Quantity = d.Detail.FQuantity,
                    ProductImage = d.ImageUrl
                }).ToList()
            };
                return new ApiResponse<OrderDetailDto> { Success = true, Message = "讀取成功", Data = dto };
        }

        // 獨立出產生付款網址的方法，讓重新付款時也能呼叫
        public async Task<string> GeneratePaymentUrlAsync(string orderNo, int paymentMethodId, decimal totalAmount)
        {
            // 透過工廠找出對應的金流服務，直接呼叫共用介面
            var paymentService = _paymentFactory.GetPaymentService(paymentMethodId);
            return await paymentService.GeneratePaymentUrlAsync(orderNo, totalAmount);
        }

        // 建立產生綠界Html 表單的方法
        public async Task<string> GenerateECPayHtmlFormAsync(string orderNo)
        {
            // 因為綠界的 ID 是 1，所以直接請工廠派 1 號服務出來產生表單
            var paymentService = _paymentFactory.GetPaymentService(1);
            return await paymentService.GenerateHtmlFormAsync(orderNo);
        }

        // 統一管理訂單狀態的文字轉換
        public string GetStatusText(int? status)
        {
            return status switch
            {
                1 => "待付款",
                2 => "已付款",
                3 => "已出貨",
                4 => "已完成",
                5 => "已取消",
                _ => "未知狀態"
            };
        }

        // 銷量報表方法
        public async Task<List<VariantSalesStatsDto>> GetSalesStatsAsync()
        {
            // 你幫他寫好這個 Query，他以後就不用自己寫 Join
            return await _db.TOrderDetails
                .GroupBy(od => od.FVariantId)
                .Select(g => new VariantSalesStatsDto
                {
                    VariantId = g.Key,
                    TotalQuantitySold = g.Sum(od => od.FQuantity),
                    TotalRevenue = g.Sum(od => od.FSubtotal)
                }).ToListAsync();
        }

        // 取消訂單並回補庫存(TODO:待組員做完後改呼叫API)
        public async Task<ApiResponse<object>> CancelOrderAsync(string orderNo)
        {
            // 1. 找出訂單
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null)
                return new ApiResponse<object> { Success = false, Message = "找不到該筆訂單" };

            if (order.FStatus != 1) // 只有待付款(1)才能取消
                return new ApiResponse<object> { Success = false, Message = "訂單狀態已變更，無法取消" };

            // 2. 開啟資料庫交易 (防護罩)
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    // A. 修改訂單狀態為「已取消」(5)
                    order.FStatus = 5;
                    order.FUpdatedAt = DateTime.Now;

                    // B. 撈出這筆訂單的所有明細
                    var details = await _db.TOrderDetails.Where(d => d.FOrderId == order.FId).ToListAsync();

                    // C. 迴圈回補庫存
                    foreach (var item in details)
                    {
                        bool isRestored = await _productService.RestoreStockAsync(item.FVariantId, item.FQuantity);
                        if (!isRestored)
                        {
                            throw new Exception($"回補庫存失敗，找不到規格 ID: {item.FVariantId}");
                        }
                    }

                    // D. 存檔並提交
                    await _db.SaveChangesAsync();
                    transaction.Commit();

                    return new ApiResponse<object> { Success = true, Message = "訂單已成功取消，庫存已回補" };
                }
                catch (Exception ex)
                {
                    // 出錯就倒回 (Rollback)
                    transaction.Rollback();
                    return new ApiResponse<object> { Success = false, Message = "取消訂單失敗：" + ex.Message };
                }
            }
        }

        // 取得單筆訂單資料
        public async Task<TOrder> GetOrderAsync(string orderNo)
        {
            return await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
        }
        
        //標記訂單為已付款(統一管理訂單狀態)
         public async Task<bool> MarkOrderAsPaidAsync(string orderNo)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order != null && order.FStatus == 1) // 1: 待付款才能變更為已付款
            {
                // 1. 更新主訂單狀態
                order.FStatus = 2; // 2: 已付款
                order.FUpdatedAt = DateTime.Now;
                // 2. 找出關聯的金流單並更新狀態 (這是你原本漏掉的關鍵！)
                var paymentTransaction = await _db.TPaymentTransactions
                    .Where(pt => pt.FOrderId == order.FId)
                    .OrderByDescending(pt => pt.FCreatedAt)
                    .FirstOrDefaultAsync();
                if (paymentTransaction != null)
                {
                    paymentTransaction.FStatus = 1; // 1: 金流付款成功
                    paymentTransaction.FPaidAt = DateTime.Now; // 壓上真實付款時間
                }
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        //=================== 以下為後台Admin專用方法 ====================
        // 取得所有訂單資料
        public async Task<object> GetAllOrdersForAdminAsync()
        {
            var orderEntities = await _db.TOrders
                .OrderByDescending(o => o.FCreatedAt)
                .ToListAsync();
            // 後台需要知道是哪位會員的訂單，我們多傳一個 MemberId
            // 並且保留原始的 Status 數字碼，方便前端下拉選單編輯
            var orders = orderEntities.Select(o => new 
            {
                OrderNo = o.FOrderNo,
                MemberId = o.FMemberId,
                TotalAmount = o.FTotalAmount,
                CreatedAt = o.FCreatedAt,
                Status = o.FStatus, 
                StatusText = GetStatusText(o.FStatus)
            }).ToList();
            
            return orders;
        }

        // 後台取的單筆訂單明細 (不需要檢查MemberId)
        public async Task<ApiResponse<OrderDetailDto>> GetOrderDetailForAdminAsync(string orderNo)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            
            if (order == null)
            {
                return new ApiResponse<OrderDetailDto> { Success = false, Message = "找不到該筆訂單" };
            }
            // 這邊的 JOIN 邏輯跟前台一模一樣
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
            // 找出付款方式
            var paymentTransaction = await _db.TPaymentTransactions
                .Where(pt => pt.FOrderId == order.FId)
                .OrderByDescending(pt => pt.FCreatedAt)
                .FirstOrDefaultAsync();
            
            string paymentMethodName = "尚未指定";
            if (paymentTransaction != null)
            {
                var method = await _db.TPaymentMethods.FirstOrDefaultAsync(m => m.FId == paymentTransaction.FMethodId);
                if (method != null)
                {
                    paymentMethodName = method.FMethodName;
                }
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
                ShippingFee = 0,
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
        
        // 後台可以強制更新訂單狀態
        public async Task<ApiResponse<object>> UpdateOrderStatusAsync(string orderNo, int newStatus)
        {
            // 防呆：如果前端試圖把狀態改為 5 (取消)，直接導向專屬的取消方法，確保庫存必定回補
            if (newStatus == 5)
            {
                return await CancelOrderForAdminAsync(orderNo);
            }
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null)
                return new ApiResponse<object> { Success = false, Message = "找不到該筆訂單" };
            order.FStatus = newStatus;
            order.FUpdatedAt = DateTime.Now;
            
            await _db.SaveChangesAsync();
            return new ApiResponse<object> { Success = true, Message = "訂單狀態更新成功" };
        }

        //後台專用：強制取消訂單並回補庫存
        public async Task<ApiResponse<object>> CancelOrderForAdminAsync(string orderNo)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null)
                return new ApiResponse<object> { Success = false, Message = "找不到該筆訂單" };
            if (order.FStatus == 5) 
                return new ApiResponse<object> { Success = false, Message = "訂單已經是取消狀態" };
            // 開啟資料庫交易 (防護罩)
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    // A. 修改狀態為 5
                    order.FStatus = 5;
                    order.FUpdatedAt = DateTime.Now;
                    // B. 撈出這筆訂單的所有明細
                    var details = await _db.TOrderDetails.Where(d => d.FOrderId == order.FId).ToListAsync();
                    // C. 迴圈回補庫存
                    foreach (var item in details)
                    {
                        bool isRestored = await _productService.RestoreStockAsync(item.FVariantId, item.FQuantity);
                        if (!isRestored)
                        {
                            throw new Exception($"回補庫存失敗，找不到規格 ID: {item.FVariantId}");
                        }
                    }
                    // D. 存檔並提交
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
    }
}

