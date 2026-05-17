using System;
using Microsoft.Extensions.DependencyInjection;

namespace Shizuku.Services
{
    // 金流服務實體工廠
    // 依據資料庫中定義的 PaymentMethodId，動態取得實作 IPaymentService 介面的金流驅動實體
    // 利用依賴注入 (DI) 動態解析，落實工廠模式 (Factory Pattern) 達到開閉原則
    public class PaymentFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public PaymentFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // 依支付方式 ID 獲取對應金流處理服務
        public IPaymentService GetPaymentService(int paymentMethodId)
        {
            return paymentMethodId switch
            {
                1 => _serviceProvider.GetRequiredService<ECPayPaymentService>(),
                2 => _serviceProvider.GetRequiredService<LinePayPaymentService>(),
                3 => _serviceProvider.GetRequiredService<CashOnDeliveryPaymentService>(),
                _ => throw new ArgumentException($"系統不支援 {paymentMethodId} 的付款方式。", nameof(paymentMethodId))
            };
        }
    }
}
