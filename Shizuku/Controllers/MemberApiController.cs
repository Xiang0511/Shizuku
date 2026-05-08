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
        public IActionResult Login([FromBody] MemberLoginRequestDto dto)
        {
            Log.Information("MemberApiController Login API");
            if (dto == null || string.IsNullOrEmpty(dto.FEmail) || string.IsNullOrEmpty(dto.FPassword))
            {
                Log.Warning("帳號或密碼為空");
                return BadRequest(new ApiResponse<MemberLoginResponseDto>
                {
                    Success = false,
                    Message = "請輸入帳號密碼"
                });
            }

            var loginDto = _memberService.Login(dto.FEmail, dto.FPassword);

            if (loginDto == null)
            {
                Log.Warning("帳號或密碼錯誤");
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
            Log.Information("MemberApiController Register API Invoked");

            if (dto == null)
            {
                Log.Warning("註冊資料為空");
                return BadRequest(new ApiResponse<MemberRegisterResponseDto> { Success = false, Message = "請提供註冊資料" });
            }

            // 1. 驗證密碼一致性
            if (dto.FPassword != dto.ConfirmPassword)
            {
                Log.Warning("註冊失敗：兩次密碼輸入不一致, Email: {Email}", dto.FEmail);
                return BadRequest(new ApiResponse<MemberRegisterResponseDto>
                {
                    Success = false,
                    Message = "兩次密碼輸入不一致"
                });
            }

            // 2. 驗證 Email 是否重複
            if (await _memberService.IsEmailTakenAsync(dto.FEmail))
            {
                Log.Warning("註冊失敗：電子信箱已被註冊, Email: {Email}", dto.FEmail);
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
                    Log.Information("註冊成功, MemberId: {MemberId}, Email: {Email}", responseData.FMemberId, dto.FEmail);
                    return Ok(new ApiResponse<MemberRegisterResponseDto>
                    {
                        Success = true,
                        Message = "註冊成功",
                        Data = responseData
                    });
                }

                Log.Error("註冊失敗：Service 回傳 null, Email: {Email}", dto.FEmail);
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
