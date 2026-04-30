namespace Shizuku.Infrastructure.Filters
{
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Logging;

    namespace Shizuku.Infrastructure.Filters
    {
        public class LogActionFilter : IActionFilter
        {
            private readonly ILogger<LogActionFilter> _logger;

            public LogActionFilter(ILogger<LogActionFilter> logger)
            {
                _logger = logger;
            }

            // 執行 Action 前
            public void OnActionExecuting(ActionExecutingContext context)
            {
                _logger.LogInformation("正在執行 API: {Controller}.{Action}",
                    context.RouteData.Values["controller"],
                    context.RouteData.Values["action"]);
            }

            // 執行 Action 後
            public void OnActionExecuted(ActionExecutedContext context)
            {
                if (context.Exception != null)
                {
                    _logger.LogError(context.Exception, "API 發生錯誤: {Controller}.{Action}",
                        context.RouteData.Values["controller"],
                        context.RouteData.Values["action"]);
                }
                else
                {
                    _logger.LogInformation("API 執行完畢: {Controller}.{Action}",
                        context.RouteData.Values["controller"],
                        context.RouteData.Values["action"]);
                }
            }
        }
    }
}
