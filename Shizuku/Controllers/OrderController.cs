using Microsoft.AspNetCore.Mvc;
using Shizuku.Services; // 記得 using 你的 Service
using Shizuku.Models.DTOs;

namespace Shizuku.Controllers
{
    // 1. 加上這些屬性，告訴系統這是一個給 Vue 用的 API Controller
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase // 2. 從 Controller 改繼承 ControllerBase (不要 View 了)
    {
        // 3. 宣告變數來裝 Service
        private readonly OrderService _orderService;

        // 4. 建構子注入 (Constructor Injection)
        // 系統管家 (DI 容器) 看到你需要 OrderService，就會自動把你剛剛在 Program.cs 註冊好的實體派過來
        public OrderController(OrderService orderService)
        {
            _orderService = orderService;
        }

        // 5. 寫一個簡單的 API 測試一下
        [HttpGet("test")]
        public IActionResult Test()
        {
            // 叫大廚做事！
            string msg = _orderService.GetTestMessage();

            // 包裝成 HTTP 200 (OK) 並轉成 JSON 回傳
            return Ok(new { Message = msg });
        }

        [HttpPost("create")]
        public IActionResult CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            CreateOrderResponseDto result = _orderService.CreateOrder(request);
            return Ok(result);
        }

    }
}
