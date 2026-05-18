using Microsoft.AspNetCore.SignalR;
using Shizuku.Models;
using System;
using System.Threading.Tasks;

namespace Shizuku.Hubs
{
    public class ChatHub : Hub
    {
        private readonly DbShizukuDemoContext _context;

        public ChatHub(DbShizukuDemoContext context)
        {
            _context = context;
        }

        public async Task JoinAsAdmin()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }

        //  新增：會員連線時，讓他加入專屬的 ID 群組
        public async Task JoinAsMember(int memberId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Member_{memberId}");
        }

        // 參數精簡：不用再傳 ConnectionId 了
        public async Task SendMessageToAdmin(int memberId, string memberName, string message)
        {
            var chatLog = new TLiveChatMessage
            {
                FMemberId = memberId,
                FSenderType = "Member",
                FSenderName = memberName,
                FMessage = message,
                FSendTime = DateTime.Now
            };
            _context.TLiveChatMessages.Add(chatLog);
            await _context.SaveChangesAsync();

            // 廣播給所有 Admin
            await Clients.Group("Admins").SendAsync("ReceiveFromMember", memberId, memberName, message);
        }

        // 參數精簡：客服回覆時，直接對著該會員的群組喊話
        public async Task ReplyToMember(int memberId, string adminName, string message)
        {
            var chatLog = new TLiveChatMessage
            {
                FMemberId = memberId,
                FSenderType = "Admin",
                FSenderName = adminName,
                FMessage = message,
                FSendTime = DateTime.Now
            };
            _context.TLiveChatMessages.Add(chatLog);
            await _context.SaveChangesAsync();

            //  關鍵：直接廣播給指定的會員群組，不管他怎麼重整都收得到！
            await Clients.Group($"Member_{memberId}").SendAsync("ReceiveFromAdmin", adminName, message);
        }
    }
}