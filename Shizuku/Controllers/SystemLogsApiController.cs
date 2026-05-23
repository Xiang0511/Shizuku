using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
using Shizuku.Services;

namespace Shizuku.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemLogsApiController : ControllerBase
    {
        private readonly SystemLogsService _logService;

        public SystemLogsApiController(SystemLogsService logService)
        {
            _logService = logService;
        }

        // GET: api/SystemLogs?skip=0&take=100
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<SystemLogResponseDto>>>> GetLogs(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100)
        {
            try
            {
                // 限制單次查詢上限，避免惡意參數攻擊伺服器
                if (take > 100) take = 100;

                var logs = await _logService.GetSystemLogsAsync(skip, take);

                return Ok(new ApiResponse<IEnumerable<SystemLogResponseDto>>
                {
                    Success = true,
                    Message = $"成功載入第 {skip + 1} 至 {skip + logs.Count()} 筆日誌",
                    Data = logs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<IEnumerable<SystemLogResponseDto>>
                {
                    Success = false,
                    Message = $"無法取得系統日誌：{ex.Message}",
                    Data = null
                });
            }
        }
    }
}
