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
    }
}
