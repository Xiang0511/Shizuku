namespace Shizuku.Services
{
    public interface IPaymentService
    {
        // 產生給前端導向的付款連結
        Task<string> GeneratePaymentUrlAsync(string orderNo, decimal totalAmount);
        
        // 產生綠界等需要 auto-submit 的 HTML 表單 (不需要的就回傳空字串)
        Task<string> GenerateHtmlFormAsync(string orderNo);
    }
}
