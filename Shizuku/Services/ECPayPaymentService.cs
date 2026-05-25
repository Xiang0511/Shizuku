using Microsoft.EntityFrameworkCore;
using Shizuku.Models;
using Shizuku.DTOs;
using Shizuku.Helpers;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Shizuku.Services
{
    // 綠界科技 (ECPay) 線上金流處理服務
    // 組裝綠界金流規格參數、進行雜湊驗證與 callback 資料解析
    // 不強耦合於 Web 框架（改用 IDictionary 代替 IFormCollection），且所有網址參數均由 appsettings.json 注入
    public class ECPayPaymentService : IPaymentService
    {
        private readonly DbShizukuDemoContext _db;
        private readonly IConfiguration _config;

        public ECPayPaymentService(DbShizukuDemoContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // 產生線上支付引導連結
        public Task<string> GeneratePaymentUrlAsync(string orderNo, decimal totalAmount)
        {
            // 由設定檔讀取後端域名，防止本機硬編碼 Port 衝突
            string backendUrl = _config["ECPay:BackendUrl"] ?? "https://localhost:7197";
            string paymentUrl = $"{backendUrl}/api/OrderApi/ecpay/{orderNo}";
            return Task.FromResult(paymentUrl);
        }

        // 產生自動轉向綠界收銀台的 HTML 表單
        public async Task<string> GenerateHtmlFormAsync(string orderNo)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null) return null;

            // 綠界交易序號 MerchantTradeNo 必須為唯一值，因此加上豪秒字尾防止重複傳送錯誤
            string tradeNoForECPay = order.FOrderNo + DateTime.Now.ToString("fff");

            string hashKey = _config["ECPay:HashKey"];
            string hashIV = _config["ECPay:HashIV"];
            string actionUrl = _config["ECPay:PaymentActionUrl"] ?? "https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5";

            var parameters = new Dictionary<string, string>
            {
                { "MerchantID", _config["ECPay:MerchantID"] },
                { "MerchantTradeNo", tradeNoForECPay },
                { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
                { "PaymentType", "aio" },
                { "TotalAmount", Convert.ToInt32(order.FTotalAmount).ToString() },
                { "TradeDesc", "Shizuku_Order" },
                { "ItemName", "Shizuku_Items" },
                { "ReturnURL", _config["ECPay:ReturnURL"] },
                { "OrderResultURL", _config["ECPay:OrderResultURL"] },
                { "ChoosePayment", "Credit" },
                { "EncryptType", "1" }
            };

            parameters["CheckMacValue"] = ECPayHelper.BuildCheckMacValue(parameters, hashKey, hashIV);

            // 寫入發送請求日誌
            var transaction = await _db.TPaymentTransactions
                .OrderByDescending(t => t.FCreatedAt)
                .FirstOrDefaultAsync(t => t.FOrderId == order.FId);

            if (transaction != null)
            {
                transaction.FGatewayTradeNo = tradeNoForECPay;

                _db.TPaymentLogs.Add(new TPaymentLog
                {
                    FPaymentTransactionsId = transaction.FId,
                    FActionType = "CreateRequest",
                    FRequestData = System.Text.Json.JsonSerializer.Serialize(parameters),
                    FCreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }

            // 產生隱藏表單與自動 Submit 的 JavaScript
            StringBuilder htmlForm = new StringBuilder();
            htmlForm.Append("<html><body>");
            htmlForm.Append($"<form id='ecpayForm' action='{actionUrl}' method='POST'>");
            foreach (var p in parameters)
            {
                htmlForm.Append($"<input type='hidden' name='{p.Key}' value='{p.Value}' />");
            }
            htmlForm.Append("</form>");
            htmlForm.Append("<script>document.getElementById('ecpayForm').submit();</script>");
            htmlForm.Append("</body></html>");

            return htmlForm.ToString();
        }

        // 驗證綠界非同步回傳結果 (解耦 IFormCollection，改用標準泛型 Dictionary 以便於單元測試)
        public bool ValidateECPayCallback(IDictionary<string, string> form, out string orderNo)
        {
            string responseJson = System.Text.Json.JsonSerializer.Serialize(form);
            orderNo = null;

            try
            {
                if (form == null || !form.ContainsKey("CheckMacValue"))
                {
                    return false;
                }

                // 1. 進行雜湊簽章 (CheckMacValue) 安全性驗證，防堵付款通知偽造
                string receivedMac = form["CheckMacValue"];
                string hashKey = _config["ECPay:HashKey"] ?? "";
                string hashIV = _config["ECPay:HashIV"] ?? "";

                var parameters = new Dictionary<string, string>();
                foreach (var kvp in form)
                {
                    if (!kvp.Key.Equals("CheckMacValue", StringComparison.OrdinalIgnoreCase))
                    {
                        parameters[kvp.Key] = kvp.Value;
                    }
                }

                string computedMac = ECPayHelper.BuildCheckMacValue(parameters, hashKey, hashIV);
                if (!string.Equals(receivedMac, computedMac, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("ECPay 簽章驗證失敗！");
                    return false;
                }

                // 2. 取得交易序號並還原真實的訂單編號
                if (form.TryGetValue("MerchantTradeNo", out string merchantTradeNo) && !string.IsNullOrEmpty(merchantTradeNo))
                {
                    // A. 先以 GatewayTradeNo 從交易記錄中搜尋對應的 Order
                    TOrder order = null;
                    var transaction = _db.TPaymentTransactions
                        .FirstOrDefault(t => t.FGatewayTradeNo == merchantTradeNo);
                    if (transaction != null)
                    {
                        order = _db.TOrders.FirstOrDefault(o => o.FId == transaction.FOrderId);
                    }

                    // B. Fallback：如果找不到，使用長度裁切還原訂單編號 (原本訂單編號為 14 碼，而 tradeNoForECPay 是 14 碼 + 3 碼毫秒 = 17 碼)
                    if (order == null && merchantTradeNo.Length > 3)
                    {
                        string fallbackOrderNo = merchantTradeNo.Substring(0, merchantTradeNo.Length - 3);
                        order = _db.TOrders.FirstOrDefault(o => o.FOrderNo == fallbackOrderNo);
                    }

                    // C. Fallback：如果再找不到，直接比對原字串
                    if (order == null)
                    {
                        order = _db.TOrders.FirstOrDefault(o => o.FOrderNo == merchantTradeNo);
                    }

                    if (order != null)
                    {
                        orderNo = order.FOrderNo;

                        // 同步寫入非同步付款通知日誌
                        var orderTx = _db.TPaymentTransactions
                            .OrderByDescending(t => t.FCreatedAt)
                            .FirstOrDefault(t => t.FOrderId == order.FId);

                        if (orderTx != null)
                        {
                            _db.TPaymentLogs.Add(new TPaymentLog
                            {
                                FPaymentTransactionsId = orderTx.FId,
                                FActionType = "Notification",
                                FResponseData = responseJson,
                                FCreatedAt = DateTime.Now
                            });
                            _db.SaveChanges();
                        }
                    }
                    else
                    {
                        // 若都找不到，以防萬一仍回傳原字串
                        orderNo = merchantTradeNo;
                    }

                    // 3. 根據綠界的回傳碼，1 代表付款成功
                    if (form.TryGetValue("RtnCode", out string rtnCode) && rtnCode == "1")
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"驗證 ECPay 發生異常: {ex.Message}");
                return false;
            }

            return false;
        }

        // 綠界模擬退款（測試環境因請款週期限制，無法即時退刷，採用模擬成功機制）
        // 若未來正式上線需串接真實退款 API，只需修改此方法即可
        public Task<ApiResponse<object>> RefundAsync(string orderNo, decimal amount, string gatewayTradeNo)
        {
            return Task.FromResult(new ApiResponse<object>
            {
                Success = true,
                Message = "綠界模擬退刷成功（測試環境無法即時退刷，狀態已同步更新）"
            });
        }
    }
}
