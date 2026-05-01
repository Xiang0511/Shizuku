using Castle.DynamicProxy;
using Serilog;

namespace Shizuku.Infrastructure.Logging
{
    public class LogInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            // 1. 執行前：紀錄進入資訊 (DEBUG)
            var methodName = invocation.Method.Name;
            var arguments = string.Join(", ", invocation.Arguments);
            Log.Debug("開始執行方法: {MethodName}, 參數: {Args}", methodName, arguments);

            try
            {
                // 2. 執行原有的方法內容
                invocation.Proceed();

                // 3. 執行後：紀錄成功 (DEBUG/INFO)
                Log.Debug("方法 {MethodName} 執行完成，回傳值: {Result}", methodName, invocation.ReturnValue);
            }
            catch (Exception ex)
            {
                // 4. 噴錯時：自動紀錄 ERROR！(這就是你要的自動化)
                Log.Error(ex, "執行方法 {MethodName} 時發生異常！訊息: {Msg}", methodName, ex.Message);
                throw; // 拋出讓全域攔截器處理
            }
        }
    }
}
