using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
using Shizuku.Services;

namespace Shizuku.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemApiController : ControllerBase
    {
        private readonly SystemService _systemService;
        public SystemApiController(SystemService systemService)
        {
            _systemService = systemService;
        }

        // PUT: api/SystemApi/config
        [HttpPut("config")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateConfig([FromBody] UpdateConfigDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.ConfigKey))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "請求參數錯誤",
                    Data = false
                });
            }

            var result = await _systemService.UpdateConfigAsync(dto);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        // GET: api/SystemApi/config
        [HttpGet("config")]
        public async Task<ActionResult<ApiResponse<SystemConfigResponseDto>>> GetSystemConfigAsync()
        {
            try
            {
                // 非同步獲取整理好的設定資料
                var configData = await _systemService.GetSystemConfigAsync();

                return Ok(new ApiResponse<SystemConfigResponseDto>
                {
                    Success = true,
                    Message = "載入系統設定資料成功",
                    Data = configData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<SystemConfigResponseDto>
                {
                    Success = false,
                    Message = $"後端載入系統設定失敗: {ex.Message}",
                    Data = null
                });
            }
        }
    }
}
