using Shizuku.DTOs;

namespace Shizuku.Services
{
    // 貨到付款 (COD) 金流處理服務
    // 實作統一金流介面 IPaymentService。
    // 因為「貨到付款」為線下實體交易，不需要像綠界 (ECPay) 或 LINE Pay 一樣導向外部金流網站，
    // 因此金流 Url 與 HTML 表單生成方法均回傳空字串，此為設計上的正常行為。
    public class CashOnDeliveryPaymentService : IPaymentService
    {
        // 產生金流支付連結 (線下交易，不需連結，回傳空字串)
        public Task<string> GeneratePaymentUrlAsync(string orderNo, decimal totalAmount)
        {
            return Task.FromResult(string.Empty);
        }

        // 產生金流 HTML 自動導向表單 (線下交易，不需表單導向，回傳空字串)
        public Task<string> GenerateHtmlFormAsync(string orderNo)
        {
            return Task.FromResult(string.Empty);
        }

        // 貨到付款退款（無金流退款機制，提醒人員手動處理現金/匯款退還）
        public Task<ApiResponse<object>> RefundAsync(string orderNo, decimal amount, string gatewayTradeNo)
        {
            return Task.FromResult(new ApiResponse<object>
            {
                Success = true,
                Message = "貨到付款退款已記錄，請引導內部人員手動退現或匯款。"
            });
        }
    }
}
