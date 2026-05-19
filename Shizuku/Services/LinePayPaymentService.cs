using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shizuku.Models;
using Shizuku.DTOs;

namespace Shizuku.Services
{
    // LINE Pay 線上支付實體服務
    // 統一金流介面 IPaymentService，處理與資料庫的交易紀錄同步、呼叫 LinePayService 通訊並記錄日誌。
    // 引導頁面 confirmUrl 與 cancelUrl 由 appsettings.json 動態配置注入。
    public class LinePayPaymentService : IPaymentService
    {
        private readonly LinePayService _linePayApi;
        private readonly DbShizukuDemoContext _db;
        private readonly IConfiguration _config;

        // 注入 LinePayService、資料庫與組態設定實體
        public LinePayPaymentService(LinePayService linePayApi, DbShizukuDemoContext db, IConfiguration config)
        {
            _linePayApi = linePayApi;
            _db = db;
            _config = config;
        }

        // 產生線上引導跳轉連結
        public async Task<string> GeneratePaymentUrlAsync(string orderNo, decimal totalAmount)
        {
            // 找出這筆訂單的支付交易紀錄
            var transaction = await _db.TPaymentTransactions
                .OrderByDescending(t => t.FCreatedAt)
                .FirstOrDefaultAsync(t => t.FOrderId == _db.TOrders.FirstOrDefault(o => o.FOrderNo == orderNo).FId);

            int payAmount = Convert.ToInt32(totalAmount);
            string confirmUrl = _config["LinePay:ConfirmUrl"] ?? "http://localhost:5173/payment/success";
            string cancelUrl = _config["LinePay:CancelUrl"] ?? "http://localhost:5173/orders";

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
                    confirmUrl = confirmUrl,
                    cancelUrl = cancelUrl
                }
            };

            // 紀錄發出的請求日誌 (TPaymentLog - Request)
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

            // 呼叫 LinePay API
            string linePayResponseJson = await _linePayApi.SendLinePayRequestAsync("/v3/payments/request", linePayPayload);

            // 紀錄收到的回應日誌 (TPaymentLog - Response)
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

            // 若綠界回傳 returnCode 為 0000 成功，更新 Gateway 交易流水號
            using (JsonDocument doc = JsonDocument.Parse(linePayResponseJson))
            {
                var root = doc.RootElement;
                if (root.GetProperty("returnCode").GetString() == "0000")
                {
                    var transactionId = root.GetProperty("info").GetProperty("transactionId").ToString();
                    transaction.FGatewayTradeNo = transactionId;

                    await _db.SaveChangesAsync();
                    return root.GetProperty("info").GetProperty("paymentUrl").GetProperty("web").GetString();
                }
                else
                {
                    string returnMessage = root.GetProperty("returnMessage").GetString();
                    throw new Exception("LINE Pay 拒絕請求：" + returnMessage);
                }
            }
        }

        // LINE Pay 不需要產生 HTML From 提交，回傳空字串即可
        public Task<string> GenerateHtmlFormAsync(string orderNo)
        {
            return Task.FromResult(string.Empty);
        }

        // 確認扣款付款狀態
        public async Task<bool> ConfirmPaymentAsync(string transactionId, string orderNo)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null) return false;

            var transaction = await _db.TPaymentTransactions
                .OrderByDescending(t => t.FCreatedAt)
                .FirstOrDefaultAsync(t => t.FOrderId == order.FId);

            var confirmPayload = new { amount = order.FTotalAmount, currency = "TWD" };
            string uri = $"/v3/payments/{transactionId}/confirm";

            // 寫入確認扣款請求日誌
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

            // 向 LINE Pay 發送確認扣款請求
            string linePayResponseJson = await _linePayApi.SendLinePayRequestAsync(uri, confirmPayload);

            // 寫入確認扣款回應日誌
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

        // LINE Pay 真實沙盒退款 API
        // 呼叫 POST /v3/payments/{transactionId}/refund 進行即時退款
        public async Task<ApiResponse<object>> RefundAsync(string orderNo, decimal amount, string gatewayTradeNo)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null)
                return new ApiResponse<object> { Success = false, Message = "找不到訂單" };

            var transaction = await _db.TPaymentTransactions
                .OrderByDescending(t => t.FCreatedAt)
                .FirstOrDefaultAsync(t => t.FOrderId == order.FId);

            int refundAmount = Convert.ToInt32(amount);
            var refundPayload = new { refundAmount };
            string uri = $"/v3/payments/{gatewayTradeNo}/refund";

            // 寫入退款請求日誌
            if (transaction != null)
            {
                _db.TPaymentLogs.Add(new TPaymentLog
                {
                    FPaymentTransactionsId = transaction.FId,
                    FActionType = "RefundRequest",
                    FRequestData = JsonSerializer.Serialize(refundPayload),
                    FCreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }

            // 呼叫 LINE Pay 退款 API
            string linePayResponseJson = await _linePayApi.SendLinePayRequestAsync(uri, refundPayload);

            // 寫入退款回應日誌
            if (transaction != null)
            {
                _db.TPaymentLogs.Add(new TPaymentLog
                {
                    FPaymentTransactionsId = transaction.FId,
                    FActionType = "RefundResponse",
                    FResponseData = linePayResponseJson,
                    FCreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }

            // 解析 LINE Pay 退款回應
            using (JsonDocument doc = JsonDocument.Parse(linePayResponseJson))
            {
                var returnCode = doc.RootElement.GetProperty("returnCode").GetString();
                var returnMessage = doc.RootElement.GetProperty("returnMessage").GetString();

                if (returnCode == "0000")
                {
                    return new ApiResponse<object> { Success = true, Message = "LINE Pay 退款成功！" };
                }
                else
                {
                    return new ApiResponse<object> { Success = false, Message = $"LINE Pay 退款失敗：{returnMessage} (代碼：{returnCode})" };
                }
            }
        }
    }
}
