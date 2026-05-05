namespace Shizuku.DTOs
{
    public class MemberRegisterDTO
    {
        public string FName { get; set; } = null!;
        public string FEmail { get; set; } = null!;
        public string FPassword { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}
