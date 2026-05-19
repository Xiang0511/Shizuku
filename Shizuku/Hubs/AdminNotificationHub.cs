using Microsoft.AspNetCore.SignalR;

namespace Shizuku.Hubs
{

    /// 後台管理員通知專用 Hub
    /// 職責：專責處理後台管理員的即時推播通知    
    public class AdminNotificationHub : Hub
    {
        // 定義後台推播群組常數，提供強型別編譯保護
        public const string GroupName = "AdminNotifications";

        // 後台員工進入管理介面時呼叫，加入後台管理員通知群組
        public async Task JoinAdminNotification()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName);
        }
    }
}
