namespace Shizuku.Services
{
    public class CashOnDeliveryPaymentService : IPaymentService
    {
        public Task<string> GeneratePaymentUrlAsync(string orderNo, decimal totalAmount)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<string> GenerateHtmlFormAsync(string orderNo)
        {
            return Task.FromResult(string.Empty);
        }
    }
}
