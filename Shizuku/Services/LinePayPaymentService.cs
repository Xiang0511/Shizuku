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
            //找出這筆訂單的支付交易
            var transaction = await _db.TPaymentTransactions
                .OrderByDescending(t => t.FCreatedAt)
                .FirstOrDefaultAsync(t => t.FOrderId == _db.TOrders.FirstOrDefault(o => o.FOrderNo == orderNo).FId);
                                    

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

            //紀錄發出的請求(TPaymentLog - Request)
            if (transaction != null)
            {
                _db.TPaymentLogs.Add(new TPaymentLog
                {
                    FPaymentTransactionsId = transaction.FId,
                    FActionType = "CreateRequest",
                    FRequestData = JsonSerializer.Serialize(linePayPayload),
                    FCreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }
            
            //呼叫 LinePay API
            string linePayResponseJson = await _linePayApi.SendLinePayRequestAsync("/v3/payments/request", linePayPayload);

            //紀錄收到的回傳內容 (TPaymentLog - Response)
            if (transaction != null)
            {
                _db.TPaymentLogs.Add(new TPaymentLog
                {
                    FPaymentTransactionsId = transaction.FId,
                    FActionType = "CreateResponse",
                    FResponseData = linePayResponseJson,
                    FCreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }

            //如果成功,把LinePay的交易序號更新回Transaction表中
            using (JsonDocument doc = JsonDocument.Parse(linePayResponseJson))
            {
                var root = doc.RootElement;
                if (root.GetProperty("returnCode").GetString() == "0000")
                {   
                    var transactionId = root.GetProperty("info").GetProperty("transactionId").ToString();
                    transaction.FGatewayTradeNo = transactionId;
                    
                    // 這裡再存一次是為了存 GatewayTradeNo
                    await _db.SaveChangesAsync(); 
                    return root.GetProperty("info").GetProperty("paymentUrl").GetProperty("web").GetString();
                }
                else
                {
                    string returnMessage = root.GetProperty("returnMessage").GetString();
                    // 這裡拋出異常沒關係，因為上面的日誌已經存好了
                    throw new Exception("LINE Pay 拒絕請求：" + returnMessage);
                }
            }
        }

        public Task<string> GenerateHtmlFormAsync(string orderNo)
        {
            return Task.FromResult(string.Empty);
        }

        // 確認扣款邏輯
        public async Task<bool> ConfirmPaymentAsync(string transactionId, string orderNo)
        {
            // 找出這筆訂單
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null) return false;
            // 找出這筆訂單對應的金流單
            var transaction = await _db.TPaymentTransactions
                .OrderByDescending(t => t.FCreatedAt)
                .FirstOrDefaultAsync(t => t.FOrderId == order.FId);
            var confirmPayload = new { amount = order.FTotalAmount, currency = "TWD" };
            string uri = $"/v3/payments/{transactionId}/confirm";
            // 寫入日誌：ConfirmRequest (確認扣款請求)
            if (transaction != null)
            {
                _db.TPaymentLogs.Add(new TPaymentLog
                {
                    FPaymentTransactionsId = transaction.FId,
                    FActionType = "ConfirmPayment",
                    FRequestData = JsonSerializer.Serialize(confirmPayload),
                    FCreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }
            // 向 LINE Pay 確認扣款
            string linePayResponseJson = await _linePayApi.SendLinePayRequestAsync(uri, confirmPayload);
            // 寫入日誌：ConfirmResponse (確認扣款回應)
            if (transaction != null)
            {
                _db.TPaymentLogs.Add(new TPaymentLog
                {
                    FPaymentTransactionsId = transaction.FId,
                    FActionType = "ConfirmResponse",
                    FResponseData = linePayResponseJson,
                    FCreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }
            using (JsonDocument doc = JsonDocument.Parse(linePayResponseJson))
            {
                return doc.RootElement.GetProperty("returnCode").GetString() == "0000";
            }
        }
    }
}
