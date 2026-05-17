using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Shizuku.Helpers
{
    // 宣告為 public static，代表這是一個純工具箱，不須 new 就能使用
    public static class ECPayHelper
    {
        public static string BuildCheckMacValue(Dictionary<string, string> parameters, string hashKey, string hashIV)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            ArgumentNullException.ThrowIfNull(hashKey);
            ArgumentNullException.ThrowIfNull(hashIV);

            var sortedKeys = parameters.Keys.OrderBy(k => k).ToList();
            var queryStrings = sortedKeys.Select(key => $"{key}={parameters[key]}");
            string rawString = string.Join("&", queryStrings);
            rawString = $"HashKey={hashKey}&{rawString}&HashIV={hashIV}";

            string urlEncodedString = HttpUtility.UrlEncode(rawString).ToLower();
            urlEncodedString = urlEncodedString.Replace("%2d", "-")
                                               .Replace("%5f", "_")
                                               .Replace("%2e", ".")
                                               .Replace("%21", "!")
                                               .Replace("%2a", "*")
                                               .Replace("%28", "(")
                                               .Replace("%29", ")")
                                               .Replace("%20", "+");

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(urlEncodedString);
                byte[] hash = sha256.ComputeHash(bytes);

                StringBuilder result = new StringBuilder();
                foreach (byte b in hash)
                {
                    result.Append(b.ToString("X2"));
                }
                return result.ToString();
            }
        }
    }
}
