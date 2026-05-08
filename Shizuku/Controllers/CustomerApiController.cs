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
            string botReply = "";

            // 加入這行：雖然目前沒有真實的資料庫 I/O 操作
            // 但這能讓編譯器知道這是一個合法的非同步方法，方便未來擴充資料庫查詢
            await Task.CompletedTask;

            // --- 機器人的「大腦」關鍵字判斷 ---
            if (userMessage.Contains("運費"))
            {
                botReply = "您好！目前全館滿 1000 元即享免運費優惠喔！未滿 1000 元，超商取貨運費為 60 元，宅配為 100 元。";
            }
            else if (userMessage.Contains("退換貨") || userMessage.Contains("退款"))
            {
                botReply = "您好！商品享有 7 天鑑賞期，若尺寸不合或有瑕疵，請保持吊牌完整，並至「訂單管理」申請退換貨即可。";
            }
            else if (userMessage.Contains("門市") || userMessage.Contains("實體店"))
            {
                botReply = "您好！目前我們是以網路電商為主，暫時沒有實體門市喔！所有的商品尺寸表都在商品頁面可以參考。";
            }
            else
            {
                // 如果都聽不懂，就給個萬用回覆
                botReply = "不好意思，我不太明白您的意思 😅。您可以嘗試詢問關於「運費」、「退換貨」或「門市」的問題，或是填寫聯絡表單，我們會盡快由專人為您解答！";
            }

            // 將答案包裝成 JSON 回傳給 Vue
            return Ok(new { reply = botReply });
        }
    }
}