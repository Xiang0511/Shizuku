using System.Text.Json;

namespace Shizuku.Services
{
    public class LinePayPaymentService : IPaymentService
    {
        private readonly LinePayService _linePayApi;

        public LinePayPaymentService(LinePayService linePayApi)
        {
            _linePayApi = linePayApi;
        }

        public async Task<string> GeneratePaymentUrlAsync(string orderNo, decimal totalAmount)
        {
            int payAmount = Convert.ToInt32(totalAmount);
            var linePayPayload = new
            {
                amount = payAmount,
                currency = "TWD",
                orderId = orderNo,
                packages = new[]
                {
                    new
                    {
                        id = "pkg_1",
                        amount = payAmount,
                        name = "Shizuku 訂單",
                        products = new[]
                        {
                            new { name = "訂單商品", quantity = 1, price = payAmount }
                        }
                    }
                },
                redirectUrls = new
                {
                    confirmUrl = "http://localhost:5173/payment/success",
                    cancelUrl = "http://localhost:5173/orders" 
                }
            };

            string linePayResponseJson = await _linePayApi.SendLinePayRequestAsync("/v3/payments/request", linePayPayload);
            using (JsonDocument doc = JsonDocument.Parse(linePayResponseJson))
            {
                var root = doc.RootElement;
                if (root.GetProperty("returnCode").GetString() == "0000")
                {
                    return root.GetProperty("info").GetProperty("paymentUrl").GetProperty("web").GetString();
                }
                else
                {
                    string returnMessage = root.GetProperty("returnMessage").GetString();
                    throw new Exception("LINE Pay 拒絕請求：" + returnMessage);
                }
            }
        }

        public Task<string> GenerateHtmlFormAsync(string orderNo)
        {
            return Task.FromResult(string.Empty);
        }
    }
}
