using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Shizuku.DTOs;
using Shizuku.Helpers;
using Shizuku.Services;
using System.IdentityModel.Tokens.Jwt;

namespace Shizuku.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberApiController : ControllerBase
    {
        private readonly MemberService _memberService;
        private readonly JwtHelper _jwtHelper;
        private readonly VerificationService _verificationService; 
        private readonly EmailService _emailService;

        public MemberApiController(MemberService memberService, JwtHelper jwtHelper, VerificationService verificationService,EmailService emailService)
        {
            _memberService = memberService;
            _jwtHelper = jwtHelper;
            _verificationService = verificationService;
            _emailService = emailService;
        }

        //登入
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

            loginDto.Token = _jwtHelper.GenerateToken(loginDto.FId, loginDto.FName ?? "", loginDto.FEmail ?? "");

            return Ok(new ApiResponse<MemberLoginResponseDto>
            {
                Success = true,
                Message = "登入成功",
                Data = loginDto
            });
        }


        //註冊
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
                // 1. 執行會員主表註冊
                var responseData = await _memberService.RegisterAsync(dto);

                if (responseData != null)
                {
                    // 2. 核心整合：產生 6 位數驗證碼並寫入 TMemberVerifications 表
                    // 這裡傳入剛才註冊成功拿到的流水號 responseData.FId
                    string code = await _verificationService.CreateEmailVerificationAsync(responseData.FId);

                    // 3. 核心整合：發送 Email 驗證碼
                    string htmlContent = $@"
                    <div style='font-family: sans-serif; max-width: 500px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                        <h2 style='color: #4a4a4a; text-align: center;'>Shizuku 購物平台</h2>
                        <hr style='border: 0; border-top: 1px solid #eee;' />
                        <p>親愛的 {responseData.FName} 您好：</p>
                        <p>感謝您註冊 Shizuku！您的 6 位數電子郵件驗證碼如下：</p>
                        <div style='background-color: #f3f4f6; padding: 15px; border-radius: 6px; text-align: center; margin: 20px 0;'>
                            <h1 style='color: #2563eb; letter-spacing: 8px; margin: 0; font-size: 36px;'>{code}</h1>
                        </div>
                        <p style='color: #666; font-size: 13px;'>請於 10 分鐘內在網頁輸入此驗證碼完成啟用。如果您沒有註冊此帳號，請忽略此信件。</p>
                    </div>";

                    await _emailService.SendEmailAsync(responseData.FEmail!, "【Shizuku】您的會員電子郵件驗證碼", htmlContent);

                    // 4. 回傳成功，把 responseData 給前端 Vue
                    // Vue 拿到後，要把 responseData.fId 存下來，等一下輸入 6 位數按送出時要一起帶過去
                    return Ok(new ApiResponse<MemberRegisterResponseDto>
                    {
                        Success = true,
                        Message = "註冊成功！驗證碼已寄發至您的信箱，請於 10 分鐘內輸入驗證碼。",
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

        //更新個人資料
        [Authorize]
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] MemberEditRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "資料格式錯誤" });
            }

            var result = await _memberService.UpdateProfileAsync(dto);

            if (result)
            {
                return Ok(new ApiResponse<string> { Success = true, Message = "個人資料已更新" });
            }

            return BadRequest(new ApiResponse<string> { Success = false, Message = "更新失敗，請確認資料是否有變動" });
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

        [Authorize]
        [HttpGet("test-header")]
        public IActionResult TestHeader()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            return Ok(new { header = authHeader });
        }
    }


}
