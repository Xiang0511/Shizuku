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
    }
}
