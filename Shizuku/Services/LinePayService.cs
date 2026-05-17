using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Shizuku.Services
{
    // LINE Pay 官方 API 通訊服務
    // 負責底層加密簽章與 HTTP 請求處理，為 LinePayPaymentService 的通訊驅動程式
    // 頻道金鑰與 API Base 網址全部由 appsettings.json 注入，不殘留任何敏感金鑰在原始碼中
    public class LinePayService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public LinePayService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        // 產生 HMAC-SHA256 簽章
        private string GenerateSignature(string uri, string requestBody, string nonce)
        {
            string channelSecret = _config["LinePay:ChannelSecret"] ?? string.Empty;
            string signatureData = channelSecret + uri + requestBody + nonce;

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(channelSecret)))
            {
                byte[] hashMessage = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureData));
                return Convert.ToBase64String(hashMessage);
            }
        }

        // 發送請求給 LINE Pay 的共用方法
        public async Task<string> SendLinePayRequestAsync(string uri, object payload)
        {
            string channelId = _config["LinePay:ChannelId"] ?? string.Empty;
            string baseUrl = _config["LinePay:BaseUrl"] ?? "https://sandbox-api-pay.line.me";

            // 將物件轉成 JSON 字串
            string requestBody = JsonSerializer.Serialize(payload);

            // 產生隨機數 (Nonce)
            string nonce = Guid.NewGuid().ToString();

            // 產生簽章
            string signature = GenerateSignature(uri, requestBody, nonce);

            // 準備 HTTP Request
            var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + uri);
            request.Headers.Add("X-LINE-ChannelId", channelId);
            request.Headers.Add("X-LINE-Authorization-Nonce", nonce);
            request.Headers.Add("X-LINE-Authorization", signature);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // 發送請求並取得回傳結果
            var response = await _httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
