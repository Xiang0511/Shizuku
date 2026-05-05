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
        [HttpPost("Submit")]
        public IActionResult SubmitTicket([FromBody] VueTicketDto dto)
        {
            // 早失敗原則
            if (dto == null)
            {
                return BadRequest(new { success = false, message = "沒有接收到資料。" });
            }

            bool isSuccess = _customerService.CreateTicketFromVue(dto);

            // 早失敗原則
            if (!isSuccess)
            {
                return BadRequest(new { success = false, message = "送出失敗，請檢查資料格式。" });
            }

            // 成功則回傳 JSON
            return Ok(new { success = true, message = "客服單已成功送出！" });
        }
    }
}