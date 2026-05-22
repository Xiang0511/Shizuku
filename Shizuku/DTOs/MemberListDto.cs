namespace Shizuku.DTOs
{
    public class MemberListDto
    {
        public int FId { get; set; }
        public string? FMemberId { get; set; }
        public string? FName { get; set; }
        public string? FEmail { get; set; }
        public string? FPhone { get; set; }
        public bool? FIsActive { get; set; }
        public int? FLevel { get; set; }
        public DateTime? FCreatedTime { get; set; }
    }
}