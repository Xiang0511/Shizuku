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
        [HttpPost("bot")]
        public async Task<IActionResult> GetBotReply([FromBody] ChatbotRequestDto dto)
        {
            // 檢查是否有防呆，避免空訊息
            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                return BadRequest("訊息不能為空");
            }

            string userMessage = dto.Message.Trim();

            // 呼叫 Service 去資料庫比對並撈取答案
            string botReply = await _customerService.GetBotResponseAsync(userMessage);

            // 將答案包裝成符合組長規範的 ApiResponse 回傳
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "成功取得機器人回覆",
                Data = new { reply = botReply }
            });
        }
        // -----------------------------------------------------------
        // 3. 前台：顧客登入後，撈取「自己的」客服表單歷史紀錄
        // -----------------------------------------------------------
        [HttpGet("History/{memberId}")]
        public async Task<IActionResult> GetMemberTickets(int memberId)
        {
            try
            {
                // Controller 只需要呼叫 Service，一行搞定邏輯！
                var tickets = await _customerService.GetMemberTicketsAsync(memberId);

                // 套用組長規範：成功、提示訊息、塞入資料
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "成功取得個人客服紀錄",
                    Data = tickets
                });
            }
            catch (Exception ex)
            {
                // 發生例外錯誤時，完美攔截並回傳 BadRequest
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "系統發生錯誤：" + ex.Message,
                    Data = null
                });
            }
        }
        // -----------------------------------------------------------
        // 後台專用：取得所有客人的聯絡表單
        // 路由：GET https://localhost:7197/api/CustomerApi/Admin/AllTickets
        // -----------------------------------------------------------
        [HttpGet("Admin/AllTickets")]
        public async Task<IActionResult> GetAllTicketsForAdmin()
        {
            try
            {
                // 呼叫我們剛剛在 Service 寫好的方法
                var tickets = await _customerService.GetAllTicketsForAdminAsync();

                // 套用你們組長的 ApiResponse 格式回傳
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "成功取得所有表單紀錄",
                    Data = tickets
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "系統發生錯誤：" + ex.Message,
                    Data = null
                });
            }
        }
        // -----------------------------------------------------------
        // 後台專用：修改客服表單狀態
        // 路由：PUT https://localhost:7197/api/CustomerApi/Admin/TicketStatus
        // -----------------------------------------------------------
        [HttpPut("Admin/TicketStatus")]
        public async Task<IActionResult> UpdateTicketStatus([FromBody] UpdateTicketStatusDto dto)
        {
            // 防呆檢查
            if (dto == null || dto.TicketId <= 0 || string.IsNullOrEmpty(dto.NewStatus))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "資料格式錯誤",
                    Data = null
                });
            }

            // 呼叫 Service 去改資料庫
            bool isSuccess = await _customerService.UpdateTicketStatusAsync(dto.TicketId, dto.NewStatus);

            if (!isSuccess)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "更新失敗，找不到該筆紀錄或發生錯誤",
                    Data = null
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = $"已成功將狀態更新為：{dto.NewStatus}",
                Data = null
            });
        }
    }
}