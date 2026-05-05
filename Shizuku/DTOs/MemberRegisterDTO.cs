using System.ComponentModel.DataAnnotations;
namespace Shizuku.DTOs
{
    public class MemberRegisterDTO
    {
        [Required(ErrorMessage = "姓名不能為空")]
        public string FName { get; set; } = null!;

        [Required(ErrorMessage = "電子郵件不能為空")]
        [EmailAddress(ErrorMessage = "電子郵件格式不正確")]
        public string FEmail { get; set; } = null!;

        [Required(ErrorMessage = "密碼不能為空")]
        [MinLength(6, ErrorMessage = "密碼長度至少需 6 個字元")]
        public string FPassword { get; set; } = null!;

        [Required(ErrorMessage = "確認密碼不能為空")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
