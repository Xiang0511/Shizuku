namespace Shizuku.DTOs
{
    public class MemberLoginResponseDTO
    {
        // 登入成功後回傳給前端顯示的名稱
        public required string FName { get; set; }

        //[cite_start]// 專業建議：未來若有 JWT Token 或權限，也可加在此處 [cite: 87]
        // public string Token { get; set; }
    }
}
