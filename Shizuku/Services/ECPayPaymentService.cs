using Microsoft.EntityFrameworkCore;
using Shizuku.Models;
using Shizuku.Helpers;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Shizuku.Services
{
    public class ECPayPaymentService : IPaymentService
    {
        private readonly DbShizukuDemoContext _db;
        private readonly IConfiguration _config;

        public ECPayPaymentService(DbShizukuDemoContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public Task<string> GeneratePaymentUrlAsync(string orderNo, decimal totalAmount)
        {
            string backendUrl = "https://localhost:7197"; // 綠界觸發轉向的後端網址
            string paymentUrl = $"{backendUrl}/api/OrderApi/ecpay/{orderNo}";
            return Task.FromResult(paymentUrl);
        }

        public async Task<string> GenerateHtmlFormAsync(string orderNo)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null) return null; 

            string tradeNoForECPay = order.FOrderNo + DateTime.Now.ToString("fff");

            string hashKey = _config["ECPay:HashKey"];
            string hashIV = _config["ECPay:HashIV"];
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

            StringBuilder htmlForm = new StringBuilder();
            htmlForm.Append("<html><body>");
            htmlForm.Append("<form id='ecpayForm' action='https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5' method='POST'>");
            foreach (var p in parameters)
            {
                htmlForm.Append($"<input type='hidden' name='{p.Key}' value='{p.Value}' />");
            }
            htmlForm.Append("</form>");
            htmlForm.Append("<script>document.getElementById('ecpayForm').submit();</script>");
            htmlForm.Append("</body></html>");

            return htmlForm.ToString();
        }

        //綠界回傳驗證結果
        public bool ValidateECPayCallback(IFormCollection form, out string orderNo)
        {
            string rtnCode = form["RtnCode"];
            string merchantTradeNo = form["MerchantTradeNo"];
            if (rtnCode == "1" && !string.IsNullOrEmpty(merchantTradeNo))
            {
                // 還原真實的訂單編號 (取前 17 碼，因為我們可能加了 fff 後綴)
                orderNo = merchantTradeNo.Length >= 17 ? merchantTradeNo.Substring(0, 17) : merchantTradeNo;
                return true;
            }
            
            orderNo = null; // C# 規定 out 參數失敗時必須給值
            return false;
        }
    }
}
