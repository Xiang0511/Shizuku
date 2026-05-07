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
        public async Task<IActionResult> SubmitTicket([FromBody] VueTicketDto dto)
        {
            if (dto == null)
            {
                //  套用組長規範：失敗、提示訊息、沒有資料 (null)
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "沒有接收到資料。",
                    Data = null
                });
            }

            bool isSuccess = await _customerService.CreateTicketFromVueAsync(dto);

            if (!isSuccess)
            {
                //  套用組長規範
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "送出失敗，請檢查資料格式。",
                    Data = null
                });
            }

            //  套用組長規範：成功、提示訊息、沒有資料 (null)
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "客服單已成功送出！",
                Data = null
            });
        }


        // -----------------------------------------------------------
        // 2. 讓 Vue 來這裡拿「問題分類」的清單 (非同步版本)
        // -----------------------------------------------------------
        [HttpGet("Categories")]
        public async Task<IActionResult> GetCategories()
        {
            // 去 Service 拿分類資料 (這段你昨天已經改成 Async 了)
            var categories = await _customerService.GetTicketCategoriesAsync();

            //  套用組長規範：成功、提示訊息、把撈出來的陣列塞進 Data 百寶箱裡！
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "取得分類成功",
                Data = categories
            });
        }
    }
}