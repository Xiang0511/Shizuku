namespace Shizuku.DTOs
{
    public class SystemLogResponseDto
    {
        public DateTime Timestamp { get; set; }
        public string? Level { get; set; }
        public string? Message { get; set; }
    }
}
