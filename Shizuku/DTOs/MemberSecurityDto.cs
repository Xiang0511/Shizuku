namespace Shizuku.DTOs
{
    public class MemberSecurityDto
    {
        // 步驟 1：請求發送驗證碼
        public class SecurityRequestCodeDto
        {
            public required string FEmail { get; set; }
            public int FType { get; set; } // 1 代表修改手機
        }

        // 步驟 2：驗證驗證碼
        public class SecurityVerifyCodeDto
        {
            public required string FEmail { get; set; }
            public required string FCode { get; set; }
            public int FType { get; set; }
        }

        // 步驟 3：變更新手機
        public class SecurityUpdatePhoneDto
        {
            public required string FNewPhone { get; set; }
            public required string FVerifiedCode { get; set; } // 安全防禦比對用
        }

        // 步驟 3：變更新生日
        public class SecurityUpdateBirthdayDto
        {
            public DateOnly FNewBirthday { get; set; }
            public required string FVerifiedCode { get; set; }
        }

        public class SecurityUpdatePasswordDto
        {
            public required string FEmail { get; set; }
            public required string FNewPassword { get; set; }
            public required string FConfirmPassword { get; set; } // 用於二次確認
            public required string FVerifiedCode { get; set; }     // 第一步留下來的驗證碼權杖
        }


    }
}
