namespace Shizuku.DTOs
{
    public class SystemConfigResponseDto
    {
        // 圖形驗證碼設定狀態
        public bool IsCaptchaActive { get; set; }
        public int CaptchaThreshold { get; set; }

        // 失敗鎖定設定狀態
        public bool IsLockoutActive { get; set; }
        public int LockoutThreshold { get; set; }
    }
}
