using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shizuku.Models;

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
                    sender = m.FSenderType == "Admin" ? $"客服 ({m.FSenderName})" : m.FSenderName,
                    text = m.FMessage,
                    isMe = m.FSenderType == "Member", // 如果是會員發的，對前台會員來說就是 "我"
                    time = m.FSendTime.ToString("HH:mm"),
                    type = m.FSenderType
                })
                .ToListAsync();

            return Ok(history);
        }
        // 取得所有曾有對話紀錄的會員清單
        [HttpGet("GetChatMembers")]
        public async Task<IActionResult> GetChatMembers()
        {
            // 從聊天紀錄表中，找出所有不重複的會員ID，並關聯會員資料表抓取姓名
            var members = await _context.TLiveChatMessages
                .Select(m => new { m.FMemberId, m.FSenderName })
                .Distinct()
                .GroupBy(m => m.FMemberId)
                .Select(g => new
                {
                    memberId = g.Key,
                    realName = g.First().FSenderName // 抓該會員最後一次使用的姓名
                })
                .ToListAsync();

            return Ok(members);
        }
    }
}