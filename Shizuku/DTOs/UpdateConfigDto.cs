namespace Shizuku.DTOs
{
    public class UpdateConfigDto
    {
        public required string ConfigKey { get; set; }
        public int FailedAttemptsThreshold { get; set; }
        public bool IsActive { get; set; }
    }
}
