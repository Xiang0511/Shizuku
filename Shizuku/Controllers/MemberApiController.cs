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
        public async Task<IActionResult> Login([FromBody] MemberLoginRequestDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.FEmail) || string.IsNullOrEmpty(dto.FPassword))
            {
                return BadRequest(new ApiResponse<MemberLoginResponseDto>
                {
                    Success = false,
                    Message = "請輸入帳號密碼"
                });
            }

            // 呼叫非同步方法並等待結果
            var loginDto = await _memberService.LoginAsync(dto.FEmail, dto.FPassword);

            if (loginDto == null)
            {
                return Unauthorized(new ApiResponse<MemberLoginResponseDto>
                {
                    Success = false,
                    Message = "帳號密碼錯誤"
                });
            }

            return Ok(new ApiResponse<MemberLoginResponseDto>
            {
                Success = true,
                Message = "登入成功",
                Data = loginDto
            });
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] MemberRegisterRequestDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new ApiResponse<MemberRegisterResponseDto> { Success = false, Message = "請提供註冊資料" });
            }

            // 1. 驗證密碼一致性
            if (dto.FPassword != dto.ConfirmPassword)
            {
                return BadRequest(new ApiResponse<MemberRegisterResponseDto>
                {
                    Success = false,
                    Message = "兩次密碼輸入不一致"
                });
            }

            // 2. 驗證 Email 是否重複
            if (await _memberService.IsEmailTakenAsync(dto.FEmail))
            {
                return Conflict(new ApiResponse<MemberRegisterResponseDto>
                {
                    Success = false,
                    Message = "此電子信箱已被註冊"
                });
            }

            // 3. 執行註冊
            try
            {
                var responseData = await _memberService.RegisterAsync(dto);

                if (responseData != null)
                {
                    return Ok(new ApiResponse<MemberRegisterResponseDto>
                    {
                        Success = true,
                        Message = "註冊成功",
                        Data = responseData
                    });
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "註冊過程中發生未預期的例外, Email: {Email}", dto.FEmail);
            }

            return StatusCode(500, new ApiResponse<MemberRegisterResponseDto>
            {
                Success = false,
                Message = "註冊過程中發生伺服器錯誤"
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
