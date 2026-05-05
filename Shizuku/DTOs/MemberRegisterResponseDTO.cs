namespace Shizuku.DTOs
{
    public class MemberRegisterResponseDTO
    {
        public string FMemberId { get; set; } = null!;
        public string FName { get; set; } = null!;
        public string FEmail { get; set; } = null!;

        public string FPhone { get; set; } = null!;
        public DateOnly? FBirthday { get; set; } = null!;
        public DateTime? FCreatedTime { get; set; }


    }
}
