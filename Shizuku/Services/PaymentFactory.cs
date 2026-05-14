using Microsoft.Extensions.DependencyInjection;

namespace Shizuku.Services
{
    public class PaymentFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public PaymentFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IPaymentService GetPaymentService(int paymentMethodId)
        {
            return paymentMethodId switch
            {
                1 => _serviceProvider.GetRequiredService<ECPayPaymentService>(),
                2 => _serviceProvider.GetRequiredService<LinePayPaymentService>(),
                3 => _serviceProvider.GetRequiredService<CashOnDeliveryPaymentService>(),
                _ => throw new Exception("系統不支援此付款方式")
            };
        }
    }
}
