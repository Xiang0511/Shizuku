namespace Shizuku.DTOs
{
    public class MemberRegisterResponseDto
    {
        public int FId { get; set; } 
        public string FMemberId { get; set; } = null!;
        public string FName { get; set; } = null!;
        public string FEmail { get; set; } = null!;

        public string FPhone { get; set; } = null!;
        public DateOnly? FBirthday { get; set; } = null!;
        public DateTime? FCreatedTime { get; set; }


    }
}
