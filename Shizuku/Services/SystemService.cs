using Microsoft.EntityFrameworkCore;
using Shizuku.DTOs;
using Shizuku.Models;

namespace Shizuku.Services
{
    public class SystemService
    {
        private readonly DbShizukuDemoContext _context;

        public SystemService(DbShizukuDemoContext context)
        {
            _context = context;
        }

        //更新系統配置規則
        public async Task<ApiResponse<bool>> UpdateConfigAsync(UpdateConfigDto dto)
        {
            var config = await _context.TSystemConfigs.FirstOrDefaultAsync(c => c.FConfigKey == dto.ConfigKey);

            if (config == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "找不到該項系統配置規則",
                    Data = false
                };
            }

            // 更新數值
            config.FFailedAttemptsThreshold = dto.FailedAttemptsThreshold;
            config.FIsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "系統配置更新成功",
                Data = true
            };
        }

        // 取得目前的系統配置規則
        // 取得目前的系統配置規則
        public async Task<SystemConfigResponseDto> GetSystemConfigAsync()
        {
            // 1. 使用主鍵直接尋找對應的規則列
            var captchaConfig = await _context.TSystemConfigs.FindAsync("Captcha");
            var lockoutConfig = await _context.TSystemConfigs.FindAsync("Lockout");

            // 2. 組裝成 DTO 回傳，同時給予安全的預設值，避免資料庫還沒建立該規則時發生錯誤
            var result = new SystemConfigResponseDto
            {
                // 圖形驗證：對應 fIsActive 與 fFailedAttemptsThreshold
                IsCaptchaActive = captchaConfig?.FIsActive ?? true,
                CaptchaThreshold = captchaConfig?.FFailedAttemptsThreshold ?? 3,

                // 失敗鎖定：對應 fIsActive 與 fFailedAttemptsThreshold
                IsLockoutActive = lockoutConfig?.FIsActive ?? true,
                LockoutThreshold = lockoutConfig?.FFailedAttemptsThreshold ?? 6
            };

            return result;
        }
    }
}
