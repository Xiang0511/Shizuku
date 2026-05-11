using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
using Shizuku.Services;
using System.Text.Json;

namespace Shizuku.Controllers
{
    //  加上這些屬性，告訴系統這是一個給 Vue 用的 API Controller
    [ApiController]
    [Route("api/[controller]")]
    public class OrderApiController : ControllerBase
    {
        // 宣告變數來裝 Service
        private readonly OrderService _orderService;
        private readonly LinePayService _linePayService;
        private readonly Models.DbShizukuDemoContext _db;

        // 建構子注入 (Constructor Injection)
        // 系統管家 (DI 容器) 看到你需要 OrderService，就會自動把你剛剛在 Program.cs 註冊好的實體派過來
        public OrderApiController(OrderService orderService, LinePayService linePayService, Models.DbShizukuDemoContext db)
        {
            _orderService = orderService;
            _linePayService = linePayService;
            _db = db;
        }

        //建立訂單API /api/orderApi/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            // 先建立訂單
            var result = await _orderService.CreateOrder(request);
            return Ok(result);
        }

        //確認付款api /api/orderApi/confirm
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequestDto request)
        {
            // 找出這筆訂單確認金額
            var order = _db.TOrders.FirstOrDefault(o => o.FOrderNo == request.OrderId);
            if (order == null) return BadRequest(new ApiResponse<object> { Success = false, Message = "找不到訂單" });
            var confirmPayload = new { amount = order.FTotalAmount, currency = "TWD" };
            string uri = $"/v3/payments/{request.TransactionId}/confirm";

            // 向 LINE Pay 確認扣款
            string linePayResponseJson = await _linePayService.SendLinePayRequestAsync(uri, confirmPayload);
            using (JsonDocument doc = JsonDocument.Parse(linePayResponseJson))
            {
                if (doc.RootElement.GetProperty("returnCode").GetString() == "0000")
                {
                    //  扣款成功！更改訂單狀態為「已付款」(假設狀態 2)
                    order.FStatus = 2;
                    order.FUpdatedAt = DateTime.Now;
                    _db.SaveChanges();
                    return Ok(new ApiResponse<object> { Success = true, Message = "付款成功！" });
                }
            }
            return BadRequest(new ApiResponse<object> { Success = false, Message = "LINE Pay 扣款失敗！" });
        }

        //讀取會員訂單列表API: /api/order/member/{memberId}  
        [HttpGet("member/{memberId}")]
        public async Task<IActionResult> GetMemberOrders(int memberId)
        {
            try
            {
                // 呼叫我們剛剛在 Service 寫好的方法，去撈這個 memberId 的訂單
                var orders = await _orderService.GetMemberOrdersAsync(memberId);

                // 把轉換好的 DTO 資料，用 Http 200 (OK) 回傳給前端
                return Ok(new ApiResponse<List<OrderListDto>>
                {
                    Success = true,
                    Message = "查詢訂單成功",
                    Data = orders
                });
            }
            catch (Exception ex)
            {
                // 如果發生錯誤，回傳 Http 400 以及錯誤訊息給前端
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "獲取訂單失敗：" + ex.Message
                });
            }
        }

        //讀取訂單明細API /api/order/{orderNo}
        [HttpGet("{orderNo}")]
        public async Task<IActionResult> GetOrderDetail(string orderNo)
        {
            var result = await _orderService.GetOrderDetailAsync(orderNo);
            
            if (!result.Success)
            {
                return NotFound(result); // 找不到訂單回傳 404
            }
            return Ok(result);
        }

        //生成綠界訂單表格API /api/orderApi/ecpay/{orderNo}
        [HttpGet("ecpay/{orderNo}")]
        public async Task<IActionResult> GenerateECPayForm(string orderNo)
        {
            //直接呼叫Service
            string htmlForm = await _orderService.GenerateECPayHtmlFormAsync(orderNo);
    
            if (string.IsNullOrEmpty(htmlForm))
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = "找不到這筆訂單" });
            }
            //成功的話，將 HTML 字串以 text/html 格式直接回傳給瀏覽器
            return Content(htmlForm, "text/html");
        }

        //綠界回傳API /api/orderApi/ecpayResult
        [HttpPost("ecpayResult")]
        public IActionResult ECPayResult([FromForm] IFormCollection form)
        {
            try
            {
                // 1. 取得綠界回傳的付款狀態 (1 代表成功)
                string rtnCode = form["RtnCode"];
                string merchantTradeNo = form["MerchantTradeNo"];

                if (rtnCode == "1" && !string.IsNullOrEmpty(merchantTradeNo))
                {
                    // 2. 還原真實的訂單編號 (取前 17 碼，因為我們可能加了 fff 後綴)
                    string orderNo = merchantTradeNo.Length >= 17 ? merchantTradeNo.Substring(0, 17) : merchantTradeNo;

                    // 3. 找出該筆訂單並更新狀態為「已付款」(2)
                    var order = _db.TOrders.FirstOrDefault(o => o.FOrderNo == orderNo);
                    if (order != null && order.FStatus == 1)
                    {
                        order.FStatus = 2; // 2: 已付款
                        order.FUpdatedAt = DateTime.Now;
                        _db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                // 若有例外發生，僅印出錯誤，確保還是能回傳 HTML 讓前端視窗正常關閉
                Console.WriteLine("ECPay 狀態更新失敗：" + ex.Message);
            }

            string html = @"
        <html>
        <body style='display:flex; justify-content:center; align-items:center; height:100vh; font-family:sans-serif;'>
            <div style='text-align:center;'>
                <h2 style='color: #4CAF50;'>付款成功！</h2>
                <p>訂單已成立，此視窗將自動關閉，請返回原畫面...</p>
            </div>
            <script>
                if (window.opener) {
                    window.opener.postMessage('PAYMENT_SUCCESS', '*');
                }
                window.close();
            </script>
        </body>
        </html>";
            return Content(html, "text/html");
        }

        //重新付款 /api/orderApi/pay/{orderNo}
        [HttpPost("pay/{orderNo}")]
        public async Task<IActionResult> RepayOrder(string orderNo, [FromBody] RepayRequestDto request)
        {
            // 找出這筆訂單
            var order = _db.TOrders.FirstOrDefault(o => o.FOrderNo == orderNo);
            if (order == null)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = "找不到該筆訂單" });
            }

            if (order.FStatus != 1)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "此訂單狀態無法重新付款" });
            }

            try
            {
                // 產生新的付款連結
                string paymentUrl = await _orderService.GeneratePaymentUrlAsync(orderNo, request.PaymentMethodId, order.FTotalAmount);
                
                return Ok(new ApiResponse<CreateOrderResponseDto>
                {
                    Success = true,
                    Message = "產生付款連結成功",
                    Data = new CreateOrderResponseDto
                    {
                        OrderNo = orderNo,
                        PaymentUrl = paymentUrl
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "產生付款連結失敗：" + ex.Message });
            }
        }

        //取消訂單 /api/orderApi/{orderNo}/cancel
        [HttpPatch("{orderNo}/cancel")]
public IActionResult CancelOrder(string orderNo)
{
    var order = _db.TOrders.FirstOrDefault(o => o.FOrderNo == orderNo);
    if (order == null)
        return NotFound(new ApiResponse<object> { Success = false, Message = "找不到該訂單" });
    if (order.FStatus != 1)
        return BadRequest(new ApiResponse<object> { Success = false, Message = "只有待付款的訂單才能取消" });
    order.FStatus = 5; // 5 = 已取消
    order.FUpdatedAt = DateTime.Now;
    _db.SaveChanges();
    return Ok(new ApiResponse<object> { Success = true, Message = "訂單已成功取消" });
}
    }

    // 放在 Controller 最下面，用來接前端的資料
    public class RepayRequestDto
    {
        public int PaymentMethodId { get; set; }
    }
    public class ConfirmPaymentRequestDto
    {
        public string TransactionId { get; set; }
        public string OrderId { get; set; }
    }
}
