using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
using Shizuku.Services;
using Shizuku.Models;

namespace Shizuku.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerApiController : ControllerBase
    {
        private readonly CustomerService _customerService;

        public CustomerApiController()
        {
            _customerService = new CustomerService(new DbShizukuDemoContext());
        }

        // Vue 透過 POST 發送表單到 https://localhost:你的Port/api/CustomerApi/Submit
        // -----------------------------------------------------------
        // 1. 接收 Vue 前台發送過來的表單資料 (非同步版本)
        // -----------------------------------------------------------
        [HttpPost("Submit")]
        //  改變 1：加上 async，回傳值變成 Task<IActionResult>
        public async Task<IActionResult> SubmitTicket([FromBody] VueTicketDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { success = false, message = "沒有接收到資料。" });
            }

            // 改變 2：呼叫 Service 的新方法，前面加上 await！
            bool isSuccess = await _customerService.CreateTicketFromVueAsync(dto);

            if (!isSuccess)
            {
                return BadRequest(new { success = false, message = "送出失敗，請檢查資料格式。" });
            }

            return Ok(new { success = true, message = "客服單已成功送出！" });
        }


        // -----------------------------------------------------------
        // 2. 讓 Vue 來這裡拿「問題分類」的清單 (非同步版本)
        // -----------------------------------------------------------
        [HttpGet("Categories")]
        // 改變 3：一樣加上 async Task<>
        public async Task<IActionResult> GetCategories()
        {
            // 改變 4：呼叫 Service 的新方法，前面加上 await！
            var categories = await _customerService.GetTicketCategoriesAsync();

            return Ok(categories);
        }
    }
}