using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shizuku.Models;
using Shizuku.DTOs; // 引入你們組長規範的 DTO 命名空間

namespace Shizuku.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatApiController : ControllerBase
    {
        private readonly DbShizukuDemoContext _context;

        public ChatApiController(DbShizukuDemoContext context)
        {
            _context = context;
        }

        // 取得指定會員的聊天歷史紀錄
        [HttpGet("GetHistory/{memberId}")]
        public async Task<IActionResult> GetHistory(int memberId)
        {
            var history = await _context.TLiveChatMessages
                .Where(m => m.FMemberId == memberId)
                .OrderBy(m => m.FSendTime) // 照時間排序，舊的在上面
                .Select(m => new {
                    sender = m.FSenderName, // ★ 改成原汁原味輸出真實名字
                    text = m.FMessage,
                    isMe = m.FSenderType == "Member", // 如果是會員發的，對前台會員來說就是 我
                    time = m.FSendTime.ToString("HH:mm"),
                    type = m.FSenderType
                })
                .ToListAsync();

            // 套用組長規範：成功、提示訊息、把陣列塞進 Data 裡面
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "成功取得聊天歷史紀錄",
                Data = history
            });
        }

        // 取得所有曾有對話紀錄的會員清單
        [HttpGet("GetChatMembers")]
        public async Task<IActionResult> GetChatMembers()
        {
            var members = await _context.TLiveChatMessages
                .Where(m => m.FSenderType == "Member")
                .Select(m => new { m.FMemberId, m.FSenderName })
                .Distinct()
                .GroupBy(m => m.FMemberId)
                .Select(g => new
                {
                    memberId = g.Key,
                    realName = g.First().FSenderName // 抓該會員最後一次使用的姓名
                })
                .ToListAsync();

            // 套用組長規範：成功、提示訊息、把清單塞進 Data 裡面
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "成功取得聊天會員清單",
                Data = members
            });
        }
    }
}