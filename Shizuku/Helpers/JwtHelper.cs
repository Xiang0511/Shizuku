using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Shizuku.Helpers
{
    public class JwtHelper
    {
        private readonly IConfiguration _configuration;

        public JwtHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 產生 JWT Token
        /// </summary>
        /// <param name="fId">使用者唯一識別碼 (PK)</param>
        /// <param name="fName">使用者名稱</param>
        /// <param name="fEmail">使用者 Email</param>
        /// <returns>JWT 字串</returns>
        public string GenerateToken(int fId, string fName, string fEmail)
        {
            // 1. 從 appsettings.json 讀取金鑰 (Key)
            // 注意：Key 至少需要 32 個字元
            var secretKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            // 2. 設定 Payload (Claims)
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, fId.ToString()), // 使用者 ID
                new Claim(JwtRegisteredClaimNames.Email, fEmail),      // Email
                new Claim("fName", fName),                             // 自定義欄位：姓名
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Token 唯一識別碼
            };

            // 3. 設定簽署憑據 (使用 HMAC SHA256)
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 4. 建立 Token 描述
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1), // 設定 1 天後過期
                signingCredentials: creds
            );

            // 5. 輸出字串
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}