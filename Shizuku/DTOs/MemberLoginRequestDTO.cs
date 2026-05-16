namespace Shizuku.DTOs
{
    public class MemberLoginRequestDto
    {
        public string FEmail { get; set; } = string.Empty;
        public string FPassword { get; set; } = string.Empty;

        // 驗證碼答案與 ID（若不需要驗證時，前端傳 null 或空字串即可）
        public string? CaptchaAnswer { get; set; }
        public string? CaptchaId { get; set; }
    }
}
