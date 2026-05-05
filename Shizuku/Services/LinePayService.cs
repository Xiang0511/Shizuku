using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Shizuku.Services
{
    public class LinePayService
    {
        private readonly HttpClient _httpClient;

        // LINE Pay Sandbox 金鑰
        private readonly string _channelId = "2009973680";
        private readonly string _channelSecret = "750f9d8ff67204413bcfbc13115d0d77";
        private readonly string _linePayBaseUrl = "https://sandbox-api-pay.line.me"; // 測試環境網址

        public LinePayService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // ---  產生 HMAC-SHA256 簽章 ---
        private string GenerateSignature(string uri, string requestBody, string nonce)
        {
            string signatureData = _channelSecret + uri + requestBody + nonce;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_channelSecret)))
            {
                byte[] hashMessage = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureData));
                return Convert.ToBase64String(hashMessage);
            }
        }

        // ---  發送請求給 LINE Pay 的共用方法 ---
        public async Task<string> SendLinePayRequestAsync(string uri, object payload)
        {
            // 將物件轉成 JSON 字串
            string requestBody = JsonSerializer.Serialize(payload);

            // 產生隨機數 (Nonce)
            string nonce = Guid.NewGuid().ToString();

            // 產生簽章
            string signature = GenerateSignature(uri, requestBody, nonce);

            // 準備 HTTP Request
            var request = new HttpRequestMessage(HttpMethod.Post, _linePayBaseUrl + uri);
            request.Headers.Add("X-LINE-ChannelId", _channelId);
            request.Headers.Add("X-LINE-Authorization-Nonce", nonce);
            request.Headers.Add("X-LINE-Authorization", signature);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // 發送請求並取得回傳結果
            var response = await _httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
