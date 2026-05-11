using System.ComponentModel.DataAnnotations;

namespace Shizuku.DTOs
{
    public class MemberEditRequestDto
    {
        [Required]
        public int FId { get; set; }

        [Required(ErrorMessage = "姓名不能為空")]
        public string FName { get; set; } = null!;

        public int? FGender { get; set; }
    }
}
