using Microsoft.AspNetCore.SignalR;

namespace Shizuku.Hubs
{

    /// 後台管理員通知專用 Hub
    /// 職責：專責處理後台管理員的即時推播通知    
    public class AdminNotificationHub : Hub
    {
        /// 後台員工進入管理介面時呼叫，加入「後台管理員通知」群組
        public async Task JoinAdminNotification()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AdminNotifications");
        }
    }
}
