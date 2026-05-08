using Microsoft.AspNetCore.SignalR;

namespace Shizuku.Hubs
{
    public class ChatHub : Hub
    {
        // 1. 客服專用：客服登入後台開啟連線時，呼叫這個方法加入「客服群組」
        public async Task JoinAsAdmin()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }

        // 2. 訪客專用：訪客傳送訊息
        public async Task SendMessageToAdmin(string message)
        {
            // Context.ConnectionId 是 SignalR 自動配發給這個訪客的唯一亂碼 (例如: aB3x9Y...)
            string guestId = Context.ConnectionId;

            // 把訪客的 ID 和訊息，推播給所有在 "Admins" 群組裡的客服人員
            await Clients.Group("Admins").SendAsync("ReceiveFromGuest", guestId, message);
        }

        // 3. 客服專用：客服指定回覆給特定訪客
        public async Task ReplyToGuest(string guestId, string message)
        {
            // 透過剛才拿到的 guestId，把訊息精準送回給那個唯一的訪客，其他人絕對看不到
            await Clients.Client(guestId).SendAsync("ReceiveFromAdmin", message);
        }
    }
}