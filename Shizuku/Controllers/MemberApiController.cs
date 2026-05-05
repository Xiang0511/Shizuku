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

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] MemberRegisterDTO dto)
        {
            // 1. 驗證密碼一致性
            if (dto.FPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "兩次密碼輸入不一致" });

            // 2. 驗證 Email 是否重複
            if (await _memberService.IsEmailTakenAsync(dto.FEmail))
                return Conflict(new { message = "此電子信箱已被註冊" });

            // 3. 執行註冊
            var success = await _memberService.RegisterAsync(dto);

            if (success)
                return Ok(new { message = "註冊成功" });

            return StatusCode(500, new { message = "註冊過程中發生錯誤" });
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
