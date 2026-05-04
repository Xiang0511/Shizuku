using Microsoft.AspNetCore.Mvc;
using Serilog;
using Shizuku.DTOs;
using Shizuku.Services;

namespace Shizuku.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberApiController : ControllerBase
    {
        private readonly MemberService _memberService;

        public MemberApiController(MemberService memberService)
        {
            _memberService = memberService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] MemberLoginRequestDTO dto)
        {
            Log.Information("MemberApiController Login API");
            if (dto == null || string.IsNullOrEmpty(dto.FEmail) || string.IsNullOrEmpty(dto.FPassword))
            {
                Log.Warning("帳號或密碼為空");
                return BadRequest(new ApiResponse<MemberLoginResponseDTO>
                {
                    Success = false,
                    Message = "請輸入帳號密碼"
                });
            }

            var loginDto = _memberService.Login(dto.FEmail, dto.FPassword);

            if (loginDto == null)
            {
                Log.Warning("帳號或密碼錯誤");
                return Unauthorized(new ApiResponse<MemberLoginResponseDTO>
                {
                    Success = false,
                    Message = "帳號密碼錯誤"
                });
            }

            return Ok(new ApiResponse<MemberLoginResponseDTO>
            {
                Success = true,
                Message = "登入成功",
                Data = loginDto
            });
        }

        [HttpGet("Lo")]
        public IActionResult Lo()
        {
            Log.Information("顯目的東西測試");
            return Ok(new
            {
                success = true,
                message = "登入成功",
            });
        }
    }
}
