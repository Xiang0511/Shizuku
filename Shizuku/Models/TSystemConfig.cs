using System.ComponentModel.DataAnnotations;

namespace Shizuku.Models
{
    public class TSystemConfig
    {
        [Key]
        public string FConfigKey { get; set; } = null!;

        public int FFailedAttemptsThreshold { get; set; }

        public bool FIsActive { get; set; }

        public string? FDescription { get; set; }
    }
}
