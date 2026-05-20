namespace Shizuku.DTOs
{
    public class MemberLoginResponseDto
    {

        public int FId { get; set; }
        // 登入成功後回傳給前端顯示的名稱
        public required string FName { get; set; }
        public string? FEmail { get; set; }
        public int? FGender { get; set; }
        public DateOnly? FBirthday { get; set; }

        public string? FPhone { get; set; }

        public int? FLevel { get; set; }

        public int? FPoints { get; set; }

        public string? FImage { get; set; }
        //[cite_start]// 專業建議：未來若有 JWT Token 或權限，也可加在此處 [cite: 87]
        // public string Token { get; set; }// 新增這一行
        public string? Token { get; set; }
    }
}
