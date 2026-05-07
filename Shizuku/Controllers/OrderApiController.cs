using Microsoft.AspNetCore.Mvc;
using Shizuku.Services; 
using Shizuku.Models.DTOs;
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

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            // 先建立訂單
            var result = await _orderService.CreateOrder(request);
            
            if (!result.Success)
            {   
                return BadRequest(result);
            }
        
            return Ok(result);
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequestDto request)
        {
            // 找出這筆訂單確認金額
            var order = _db.TOrders.FirstOrDefault(o => o.FOrderNo == request.OrderId);
            if (order == null) return BadRequest(new { Success  = false, Message = "找不到訂單" });
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
                    return Ok(new { Success  = true, Message = "付款成功！" });
                }
            }
            return BadRequest(new { Success  = false, Message = "LINE Pay 扣款失敗！" });
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
                return Ok(new ApiResponse<List<OrderListDto>> { 
                    Success = true, 
                    Message = "查詢訂單成功", 
                    Data = orders 
                });
            }
            catch (Exception ex)
            {
                // 如果發生錯誤，回傳 Http 400 以及錯誤訊息給前端
                return BadRequest(new ApiResponse<object> { 
                    Success = false, 
                    Message = "獲取訂單失敗：" + ex.Message 
                });
            }
        }
    }

    // 放在 Controller 最下面，用來接前端的資料
    public class ConfirmPaymentRequestDto
    {
        public string TransactionId { get; set; }
        public string OrderId { get; set; }
    }
}
