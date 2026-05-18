using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Shizuku.Models; // 確保有引入你的 Models 命名空間
using System;

namespace Shizuku.Hubs
{
    public class ChatHub : Hub
    {
        // 宣告一個唯讀的變數來裝資料庫連線
        private readonly DbShizukuDemoContext _context;

        // 透過「依賴注入」把資料庫叫進來
        public ChatHub(DbShizukuDemoContext context)
        {
            _context = context;
        }

        public async Task JoinAsAdmin()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }

        //  修改 1：多接了一個 int memberId
        public async Task SendMessageToAdmin(int memberId, string memberName, string message)
        {
            string guestId = Context.ConnectionId;

            // --- 1. 寫入資料庫 ---
            var chatLog = new TLiveChatMessage
            {
                FMemberId = memberId,
                FSenderType = "Member",
                FSenderName = memberName,
                FMessage = message,
                FSendTime = DateTime.Now
            };
            _context.TLiveChatMessages.Add(chatLog);
            await _context.SaveChangesAsync(); // 存檔！

            // --- 2. 廣播給客服 --- (多把 memberId 傳給後台)
            await Clients.Group("Admins").SendAsync("ReceiveFromMember", guestId, memberId, memberName, message);
        }

        //  修改 2：多接了一個 int memberId
        public async Task ReplyToMember(string guestId, int memberId, string adminName, string message)
        {
            // --- 1. 寫入資料庫 ---
            var chatLog = new TLiveChatMessage
            {
                FMemberId = memberId,
                FSenderType = "Admin",
                FSenderName = adminName,
                FMessage = message,
                FSendTime = DateTime.Now
            };
            _context.TLiveChatMessages.Add(chatLog);
            await _context.SaveChangesAsync(); // 存檔！

            // --- 2. 廣播給會員 ---
            await Clients.Client(guestId).SendAsync("ReceiveFromAdmin", adminName, message);
        }
    }
}