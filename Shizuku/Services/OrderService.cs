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

        // 這是建構子 (Constructor)
        // 當 DI 容器要建立 OrderService 時，發現它需要 DbShizukuDemoContext 和 LinePayService，就會自動塞進來
        public OrderService(DbShizukuDemoContext db, LinePayService linePayService)
        {
            _db = db;
            _linePayService = linePayService;
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

                        // 4-2: 檢查庫存 (FStock) 夠不夠？
                        if (variant.FStock < item.Quantity)
                        {
                            throw new Exception("很抱歉，部分商品庫存不足，被搶走啦！");
                        }

                        // 4-3: 庫存夠的話，馬上扣除庫存！
                        variant.FStock -= item.Quantity;

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
public async Task<ApiResponse<OrderDetailDto>> GetOrderDetailAsync(string orderNo)
{
    // 1. 先單獨把訂單主表撈出來
    var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
    
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
        // 如果未來有建立訂單與付款方式的關聯，這裡再抽換
        PaymentMethod = "尚未指定", 
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
            string paymentUrl = string.Empty;

            switch (paymentMethodId)
            {
                case 1: // 綠界科技 ECPay
                    string backendUrl = "https://localhost:7197";
                    paymentUrl = $"{backendUrl}/api/OrderApi/ecpay/{orderNo}";
                    break;

                case 2: // LINE Pay
                    int payAmount = Convert.ToInt32(totalAmount);
                    var linePayPayload = new
                    {
                        amount = payAmount,
                        currency = "TWD",
                        orderId = orderNo,
                        packages = new[]
                        {
                            new
                            {
                                id = "pkg_1",
                                amount = payAmount,
                                name = "Shizuku 訂單",
                                products = new[]
                                {
                                    new { name = "訂單商品", quantity = 1, price = payAmount }
                                }
                            }
                        },
                        redirectUrls = new
                        {
                            confirmUrl = "http://localhost:5173/payment/success",
                            cancelUrl = "http://localhost:5173/orders" // 取消改回傳訂單列表
                        }
                    };

                    string linePayResponseJson = await _linePayService.SendLinePayRequestAsync("/v3/payments/request", linePayPayload);
                    using (JsonDocument doc = JsonDocument.Parse(linePayResponseJson))
                    {
                        var root = doc.RootElement;
                        if (root.GetProperty("returnCode").GetString() == "0000")
                        {
                            paymentUrl = root.GetProperty("info").GetProperty("paymentUrl").GetProperty("web").GetString();
                        }
                        else
                        {
                            string returnMessage = root.GetProperty("returnMessage").GetString();
                            throw new Exception("LINE Pay 拒絕請求：" + returnMessage);
                        }
                    }
                    break;

                case 3: // 貨到付款
                    paymentUrl = string.Empty;
                    break;

                default:
                    throw new Exception("系統不支援此付款方式");
            }

            return paymentUrl;
        }

        //建立產生綠界Html 表單的方法
        public async Task<string> GenerateECPayHtmlFormAsync(string orderNo)
{
    // 1. 去資料庫找這筆訂單
    var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
    if (order == null) return null; // 找不到訂單就回傳 null
    string tradeNoForECPay = order.FOrderNo + DateTime.Now.ToString("fff");

    // 2. 準備綠界 API 需要的參數
    var parameters = new Dictionary<string, string>
    {
        { "MerchantID", "3002607" },
        { "MerchantTradeNo", tradeNoForECPay }, 
        { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
        { "PaymentType", "aio" },
        { "TotalAmount", Convert.ToInt32(order.FTotalAmount).ToString() },
        { "TradeDesc", "Shizuku_Order" }, 
        { "ItemName", "Shizuku_Items" }, 
        { "ReturnURL", "https://localhost:7197/api/OrderApi/ecpayReturn" }, 
        { "OrderResultURL", "https://localhost:7197/api/OrderApi/ecpayResult" }, 
        { "ChoosePayment", "Credit" },
        { "EncryptType", "1" }
    };
    // 3. 計算 CheckMacValue (壓碼)
    string hashKey = "pwFHCqoQZGmho4w6"; 
    string hashIV = "EkRm7iFT261dpevs";  
    parameters["CheckMacValue"] = BuildCheckMacValue(parameters, hashKey, hashIV);
    // 4. 產生帶有自動送出功能的 HTML 字串
    StringBuilder htmlForm = new StringBuilder();
    htmlForm.Append("<html><body>");
    htmlForm.Append("<form id='ecpayForm' action='https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5' method='POST'>");
    
    foreach (var p in parameters)
    {
        htmlForm.Append($"<input type='hidden' name='{p.Key}' value='{p.Value}' />");
    }
    
    htmlForm.Append("</form>");
    htmlForm.Append("<script>document.getElementById('ecpayForm').submit();</script>");
    htmlForm.Append("</body></html>");
    return htmlForm.ToString();
}
// 這個小工具也是放在 OrderService 裡面，設為 private 讓內部呼叫即可
private string BuildCheckMacValue(Dictionary<string, string> parameters, string hashKey, string hashIV)
{
    var sortedKeys = parameters.Keys.OrderBy(k => k).ToList();
    var queryStrings = sortedKeys.Select(key => $"{key}={parameters[key]}");
    string rawString = string.Join("&", queryStrings);
    rawString = $"HashKey={hashKey}&{rawString}&HashIV={hashIV}";
    string urlEncodedString = HttpUtility.UrlEncode(rawString).ToLower();
    urlEncodedString = urlEncodedString.Replace("%2d", "-")
                                       .Replace("%5f", "_")
                                       .Replace("%2e", ".")
                                       .Replace("%21", "!")
                                       .Replace("%2a", "*")
                                       .Replace("%28", "(")
                                       .Replace("%29", ")")
                                       .Replace("%20", "+");
    using (SHA256 sha256 = SHA256.Create())
    {
        byte[] bytes = Encoding.UTF8.GetBytes(urlEncodedString);
        byte[] hash = sha256.ComputeHash(bytes);
        
        StringBuilder result = new StringBuilder();
        foreach (byte b in hash)
        {
            result.Append(b.ToString("X2"));
        }
        return result.ToString();
    }
}

        
        // 統一管理訂單狀態的文字轉換
        private string GetStatusText(int? status)
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
    }

}

