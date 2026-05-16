using System.ComponentModel.DataAnnotations;
namespace Shizuku.DTOs
{
    public class MemberRegisterRequestDto
    {
        [Required(ErrorMessage = "姓名不能為空")]
        public string FName { get; set; } = null!;

        [Required(ErrorMessage = "電子郵件不能為空")]
        [EmailAddress(ErrorMessage = "電子郵件格式不正確")]
        public string FEmail { get; set; } = null!;

        [Required(ErrorMessage = "電話號碼不能為空")]
        [RegularExpression(@"^09\d{8}$", ErrorMessage = "電話號碼格式不正確")]
        public string FPhone { get; set; } = null!;

        [Required(ErrorMessage = "請選擇性別")]
        public int? FGender { get; set; } = null!;

        [Required(ErrorMessage = "生日日期不能為空")]
        public DateOnly? FBirthday { get; set; } = null!;

        [Required(ErrorMessage = "密碼不能為空")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "密碼長度必須在 8 到 100 個字元之間")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "密碼必須包含大寫字母、小寫字母、數字及特殊符號各至少一個")]
        public string FPassword { get; set; } = null!;

        [Required(ErrorMessage = "確認密碼不能為空")]
        [Compare("FPassword", ErrorMessage = "密碼與確認密碼不一致")]
        public string ConfirmPassword { get; set; } = null!;


    }
}
