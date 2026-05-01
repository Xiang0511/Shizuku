using Castle.DynamicProxy;
using Serilog;

namespace Shizuku.Infrastructure.Logging
{
    public class LogInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            var methodName = invocation.Method.Name;
            var arguments = string.Join(", ", invocation.Arguments);
            Log.Debug("開始執行: {MethodName}, 參數: {Args}", methodName, arguments);

            try
            {
                invocation.Proceed();

                // --- 進階自動化：檢查回傳值 ---
                if (invocation.ReturnValue == null)
                {
                    // 如果 Service 回傳 null，自動記一筆警告，你就不用在 Controller 寫了！
                    Log.Warning("方法 {MethodName} 執行結果為空 (可能帳密錯誤或查無資料)", methodName);
                }
                else
                {
                    Log.Debug("方法 {MethodName} 成功執行", methodName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "方法 {MethodName} 崩潰！", methodName);
                throw;
            }
        }
    }
}
