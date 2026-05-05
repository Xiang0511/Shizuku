using Microsoft.AspNetCore.Mvc;
using Shizuku.Services; 
using Shizuku.Models.DTOs;
using System.Text.Json;

namespace Shizuku.Controllers
{
    //  加上這些屬性，告訴系統這是一個給 Vue 用的 API Controller
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase 
    {
        // 宣告變數來裝 Service
        private readonly OrderService _orderService;
        private readonly LinePayService _linePayService;
        private readonly Models.DbShizukuDemoContext _db;

        // 建構子注入 (Constructor Injection)
        // 系統管家 (DI 容器) 看到你需要 OrderService，就會自動把你剛剛在 Program.cs 註冊好的實體派過來
        public OrderController(OrderService orderService, LinePayService linePayService, Models.DbShizukuDemoContext db)
        {
            _orderService = orderService;
            _linePayService = linePayService;
            _db = db;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            CreateOrderResponseDto result = await _orderService.CreateOrder(request);
            return Ok(result);
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequestDto request)
        {
            // 找出這筆訂單確認金額
            var order = _db.TOrders.FirstOrDefault(o => o.FOrderNo == request.OrderId);
            if (order == null) return BadRequest(new { IsSuccess = false, Message = "找不到訂單" });
            var confirmPayload = new { amount = order.FTotalAmount, currency = "TWD" };
            string uri = $"/v3/payments/{request.TransactionId}/confirm";
            
            // 向 LINE Pay 確認扣款
            string linePayResponseJson = await _linePayService.SendLinePayRequestAsync(uri, confirmPayload);
            using (JsonDocument doc = JsonDocument.Parse(linePayResponseJson))
            {
                if (doc.RootElement.GetProperty("returnCode").GetString() == "0000")
                {
                    // 🌟 扣款成功！更改訂單狀態為「已付款」(假設狀態 2)
                    order.FStatus = 2; 
                    order.FUpdatedAt = DateTime.Now;
                    _db.SaveChanges();
                    return Ok(new { IsSuccess = true, Message = "付款大成功！" });
                }
            }
            return BadRequest(new { IsSuccess = false, Message = "LINE Pay 扣款失敗！" });
        }
    }
    // 放在 Controller 最下面，用來接前端的資料
    public class ConfirmPaymentRequestDto
    {
        public string TransactionId { get; set; }
        public string OrderId { get; set; }
    }
}
