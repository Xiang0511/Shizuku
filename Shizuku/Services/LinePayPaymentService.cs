using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shizuku.Models;

namespace Shizuku.Services
{
    public class LinePayPaymentService : IPaymentService
    {
        private readonly LinePayService _linePayApi;
        private readonly DbShizukuDemoContext _db;

        // 注入 LinePayService 以及資料庫實體
        public LinePayPaymentService(LinePayService linePayApi, DbShizukuDemoContext db)
        {
            _linePayApi = linePayApi;
            _db = db;
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

        // --- 新增：確認扣款邏輯 ---
        public async Task<bool> ConfirmPaymentAsync(string transactionId, string orderNo)
        {
            // 找出這筆訂單確認金額
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null) return false;

            var confirmPayload = new { amount = order.FTotalAmount, currency = "TWD" };
            string uri = $"/v3/payments/{transactionId}/confirm";

            // 向 LINE Pay 確認扣款
            string linePayResponseJson = await _linePayApi.SendLinePayRequestAsync(uri, confirmPayload);
            using (JsonDocument doc = JsonDocument.Parse(linePayResponseJson))
            {
                return doc.RootElement.GetProperty("returnCode").GetString() == "0000";
            }
        }
    }
}
