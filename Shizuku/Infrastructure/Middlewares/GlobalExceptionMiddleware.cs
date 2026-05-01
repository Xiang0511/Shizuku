namespace Shizuku.Infrastructure.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                // 讓請求繼續往下走 (走到 Controller -> Service)
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                // --- 這裡就是你要的自動化 ---
                // 只要後端任何一個地方噴 Exception，這裡會自動捕捉並紀錄為 ERROR 等級
                Serilog.Log.Error(ex, "系統發生未預期錯誤！來源：{Path}", httpContext.Request.Path);

                // 處理完 Log 後，可以選擇導向錯誤頁面或回傳 JSON
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500;
            return context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "伺服器發生錯誤，請稍後再試"
            });
        }
    }
}