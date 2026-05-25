using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using Shizuku.DTOs;
using Shizuku.Helpers;
using Shizuku.Services;
using SkiaSharp;
using System.IdentityModel.Tokens.Jwt;
using static Shizuku.DTOs.MemberSecurityDto;

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
        private readonly IMemoryCache _cache;

        public MemberApiController(
            MemberService memberService,
            JwtHelper jwtHelper, 
            VerificationService verificationService,
            EmailService emailService,
            IMemoryCache cache
            )
        {
            _memberService = memberService;
            _jwtHelper = jwtHelper;
            _verificationService = verificationService;
            _emailService = emailService; 
            _cache = cache;
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

            var serviceResult = await _memberService.LoginAsync(dto);

            if (!serviceResult.Success)
            {
                return Unauthorized(serviceResult);
            }

            var loginDto = serviceResult.Data!;
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

            if (dto.FPassword != dto.ConfirmPassword)
            {
                return BadRequest(new ApiResponse<MemberRegisterResponseDto>
                {
                    Success = false,
                    Message = "兩次密碼輸入不一致"
                });
            }

            if (await _memberService.IsEmailTakenAsync(dto.FEmail))
            {
                return Conflict(new ApiResponse<MemberRegisterResponseDto>
                {
                    Success = false,
                    Message = "此電子信箱已被註冊"
                });
            }

            try
            {
                var responseData = await _memberService.RegisterAsync(dto);

                if (responseData != null)
                {
                    string code = await _verificationService.CreateEmailVerificationAsync(responseData.FId);

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

        [HttpGet("captcha")]
        public IActionResult GetCaptcha()
        {
            // 1. 產生 4 碼隨機英數字（排除容易混淆的 0, o, 1, I）
            string chars = "abcdefhkmnrstuvwx3456789ABCDEFGHJKLMNPRSTUVWXY";
            var random = new Random();
            string captchaText = new string(Enumerable.Repeat(chars, 4).Select(s => s[random.Next(s.Length)]).ToArray());

            // 2. 產生唯一的 CaptchaId 並將答案寫入 MemoryCache（效期 2 分鐘）
            string captchaId = Guid.NewGuid().ToString();
            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(2));
            _cache.Set($"Captcha_{captchaId}", captchaText, cacheOptions);

            // 3. 使用 SkiaSharp 繪製扭曲圖片
            int width = 120;
            int height = 45;
            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);

            // 背景填滿淺灰色
            canvas.Clear(new SKColor(245, 245, 245));

            // 畫幾條隨機干擾線
            using var linePaint = new SKPaint { Color = new SKColor(200, 200, 200), StrokeWidth = 2, IsAntialias = true };
            for (int i = 0; i < 4; i++)
            {
                canvas.DrawLine(random.Next(width), random.Next(height), random.Next(width), random.Next(height), linePaint);
            }

            // 畫隨機干擾點
            for (int i = 0; i < 30; i++)
            {
                bitmap.SetPixel(random.Next(width), random.Next(height), new SKColor(180, 180, 180));
            }

            // 寫入驗證碼文字（使用全新的 SKFont 與 SKTextBlob，避開過時 API）
            string[] fontFamilies = { "Arial", "Verdana", "Comic Sans MS" };

            for (int i = 0; i < captchaText.Length; i++)
            {
                // 1. 設定顏色與抗鋸齒（SKPaint 現在純粹負責顏色、樣式與濾鏡）
                using var textPaint = new SKPaint
                {
                    Color = new SKColor((byte)random.Next(50, 150), (byte)random.Next(50, 150), (byte)random.Next(50, 150)),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };

                // 2. 建立字體樣式
                var typeface = SKTypeface.FromFamilyName(
                    fontFamilies[random.Next(fontFamilies.Length)],
                    SKFontStyleWeight.Bold,
                    SKFontStyleWidth.Normal,
                    SKFontStyleSlant.Upright
                );

                // 3. ✨ 建立全新的 SKFont 物件來設定字體大小
                using var textFont = new SKFont(typeface, 26); // 這裡設定文字大小為 26

                canvas.Save();

                // 隨機旋轉角度 (-15度 ~ 15度)
                float angle = random.Next(-15, 15);
                float x = 15 + (i * 25);
                float y = 32 + random.Next(-3, 3);

                canvas.RotateDegrees(angle, x, y);

                // 4. ✨ 使用最新的 DrawText 載入 font 與 paint 繪製單個字元
                canvas.DrawText(captchaText[i].ToString(), x, y, textFont, textPaint);

                canvas.Restore();
            }

            // 4. 將圖片轉為 Base64 字串
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);
            using var ms = new MemoryStream();
            data.SaveTo(ms);
            string base64Image = Convert.ToBase64String(ms.ToArray());

            // 5. 回傳給前端（包含圖片與對應的 ID）
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "驗證碼產生成功",
                Data = new
                {
                    CaptchaId = captchaId,
                    ImgBase64 = $"data:image/png;base64,{base64Image}"
                }
            });
        }

        // 步驟 1：發送修改資料驗證碼
        [Authorize]
        [HttpPost("security/request-code")]
        public async Task<IActionResult> RequestSecurityCode([FromBody] MemberSecurityDto.SecurityRequestCodeDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.FEmail))
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "請輸入有效的 Email" });
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int memberId))
            {
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "未授權的存取" });
            }

            var serviceResult = await _memberService.GenerateSecurityCodeAsync(memberId, dto.FEmail, dto.FType);
            if (!serviceResult.Success)
            {
                return BadRequest(serviceResult);
            }

            string code = serviceResult.Data!;
            string typeName = dto.FType switch
            {
                1 => "手機號碼",
                2 => "會員生日",
                3 => "登入密碼", // 新增 Type 3 的電子郵件文字
                _ => "安全資料"
            };
            string emailSubject = $"【Shizuku】安全變更：{typeName}修改驗證信";

            string htmlContent = $@"
        <div style='font-family: sans-serif; max-width: 500px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
            <h2 style='color: #4a4a4a; text-align: center;'>Shizuku 購物平台</h2>
            <hr style='border: 0; border-top: 1px solid #eee;' />
            <p>您好：</p>
            <p>您正在進行變更 {typeName} 的身分驗證。您的 6 位數安全驗證碼如下：</p>
            <div style='background-color: #f3f4f6; padding: 15px; border-radius: 6px; text-align: center; margin: 20px 0;'>
                <h1 style='color: #2563eb; letter-spacing: 8px; margin: 0; font-size: 36px;'>{code}</h1>
            </div>
            <p style='color: #666; font-size: 13px;'>請於 10 分鐘內在網頁輸入此驗證碼。如果您沒有要求變更此資料，請立即忽略並檢查帳號安全。</p>
        </div>";

            try
            {
                await _emailService.SendEmailAsync(dto.FEmail, emailSubject, htmlContent);
                return Ok(new ApiResponse<string> { Success = true, Message = "驗證碼已成功寄發至您的信箱。" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "修改 {TypeName} 發送郵件失敗, MemberId: {MemberId}, Email: {Email}", typeName, memberId, dto.FEmail);
                return StatusCode(500, new ApiResponse<string> { Success = false, Message = "郵件伺服器發送失敗，請稍後再試" });
            }
        }

        // 步驟 2：驗證安全驗證碼
        [Authorize]
        [HttpPost("security/verify-code")]
        public async Task<IActionResult> VerifySecurityCode([FromBody] MemberSecurityDto.SecurityVerifyCodeDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.FCode))
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "請輸入驗證碼" });
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int memberId))
            {
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "未授權的存取" });
            }

            var result = await _memberService.VerifySecurityCodeAsync(memberId, dto.FCode, dto.FType);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // 步驟 3：確認變更手機
        [Authorize]
        [HttpPost("security/update-phone")]
        public async Task<IActionResult> UpdatePhone([FromBody] MemberSecurityDto.SecurityUpdatePhoneDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.FNewPhone))
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "請輸入新手機號碼" });
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int memberId))
            {
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "未授權的存取" });
            }

            var result = await _memberService.UpdatePhoneAsync(memberId, dto.FNewPhone, dto.FVerifiedCode);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // 步驟 3：確認變更生日
        [Authorize]
        [HttpPost("security/update-birthday")]
        public async Task<IActionResult> UpdateBirthday([FromBody] MemberSecurityDto.SecurityUpdateBirthdayDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "請輸入有效的變更資料" });
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int memberId))
            {
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "未授權的存取" });
            }

            var result = await _memberService.UpdateBirthdayAsync(memberId, dto.FNewBirthday, dto.FVerifiedCode);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // 步驟 3：確認變更密碼
        [Authorize]
        [HttpPost("security/update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] MemberSecurityDto.SecurityUpdatePasswordDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.FNewPassword) || string.IsNullOrEmpty(dto.FConfirmPassword))
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "請輸入完整的新密碼與確認密碼" });
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int memberId))
            {
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "未授權的存取" });
            }

            // 呼叫 Service
            var result = await _memberService.UpdatePasswordAsync(memberId, dto.FNewPassword, dto.FConfirmPassword, dto.FVerifiedCode);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // 1. 忘記密碼發送驗證碼
        [AllowAnonymous] // 加上這個，允許未登入存取
        [HttpPost("forgot-password/request-code")]
        public async Task<IActionResult> ForgotRequestSecurityCode([FromBody] MemberSecurityDto.SecurityRequestCodeDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.FEmail))
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "請輸入有效的 Email" });
            }

            // 【修改點】改用 Email 去 Service 查出 MemberId，不再從 User.FindFirst 抓取
            var memberResult = await _memberService.GetMemberIdByEmailAsync(dto.FEmail);
            if (!memberResult.Success)
            {
                // 安全防禦：這裡也可以故意回傳成功，避免惡意人士探測 Email 是否註冊
                return BadRequest(new ApiResponse<string> { Success = false, Message = "找不到該 Email 綁定的會員" });
            }
            int memberId = memberResult.Data;

            var serviceResult = await _memberService.GenerateSecurityCodeAsync(memberId, dto.FEmail, dto.FType);
            if (!serviceResult.Success)
            {
                return BadRequest(serviceResult);
            }

            string code = serviceResult.Data!;
            string typeName = dto.FType switch
            {
                1 => "手機號碼",
                2 => "會員生日",
                3 => "登入密碼",
                _ => "安全資料"
            };
            string emailSubject = $"【Shizuku】安全變更：{typeName}修改驗證信";

            string htmlContent = $@"
    <div style='font-family: sans-serif; max-width: 500px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
        <h2 style='color: #4a4a4a; text-align: center;'>Shizuku 購物平台</h2>
        <hr style='border: 0; border-top: 1px solid #eee;' />
        <p>您好：</p>
        <p>您正在進行變更 {typeName} 的身分驗證。您的 6 位數安全驗證碼如下：</p>
        <div style='background-color: #f3f4f6; padding: 15px; border-radius: 6px; text-align: center; margin: 20px 0;'>
            <h1 style='color: #2563eb; letter-spacing: 8px; margin: 0; font-size: 36px;'>{code}</h1>
        </div>
        <p style='color: #666; font-size: 13px;'>請於 10 分鐘內在網頁輸入此驗證碼。如果您沒有要求變更此資料，請立即忽略並檢查帳號安全。</p>
    </div>";

            try
            {
                await _emailService.SendEmailAsync(dto.FEmail, emailSubject, htmlContent);
                return Ok(new ApiResponse<string> { Success = true, Message = "驗證碼已成功寄發至您的信箱。" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "修改 {TypeName} 發送郵件失敗, MemberId: {MemberId}, Email: {Email}", typeName, memberId, dto.FEmail);
                return StatusCode(500, new ApiResponse<string> { Success = false, Message = "郵件伺服器發送失敗，請稍後再試" });
            }
        }

        // 2. 忘記密碼驗證認證碼
        [AllowAnonymous] // 加上這個
        [HttpPost("forgot-password/verify-code")]
        public async Task<IActionResult> ForgotVerifySecurityCode([FromBody] MemberSecurityDto.SecurityVerifyCodeDto dto)
        {
            // 雖然這裡前端傳進來的 DTO 沒有 FEmail，建議你在 DTO 中加上 FEmail
            // 或是把對應的資訊也改成從系統內查找，這裡必須先查到 memberId
            if (dto == null || string.IsNullOrEmpty(dto.FCode) || string.IsNullOrEmpty(dto.FEmail))
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "請提供完整驗證資訊" });
            }

            var memberResult = await _memberService.GetMemberIdByEmailAsync(dto.FEmail);
            if (!memberResult.Success)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "驗證失敗" });
            }
            int memberId = memberResult.Data;

            var result = await _memberService.VerifySecurityCodeAsync(memberId, dto.FCode, dto.FType);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // 3. 忘記密碼重設
        [AllowAnonymous] // 加上這個
        [HttpPost("forgot-password/reset")]
        public async Task<IActionResult> ForgotPassword([FromBody] MemberSecurityDto.SecurityUpdatePasswordDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.FNewPassword) || string.IsNullOrEmpty(dto.FConfirmPassword) || string.IsNullOrEmpty(dto.FEmail))
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "請輸入完整欄位資訊" });
            }

            var memberResult = await _memberService.GetMemberIdByEmailAsync(dto.FEmail);
            if (!memberResult.Success)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = "帳號無效" });
            }
            int memberId = memberResult.Data;

            // 呼叫 Service
            var result = await _memberService.UpdatePasswordAsync(memberId, dto.FNewPassword, dto.FConfirmPassword, dto.FVerifiedCode);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // 更新照片
        [HttpPost("{memberId}/upload-avatar")]
        public async Task<ActionResult<ApiResponse<string>>> UploadAvatar(int memberId, IFormFile file)
        {
            var result = await _memberService.UploadAvatarAsync(memberId, file);

            if (!result.Success)
            {
                return BadRequest(result); // 400 錯誤
            }

            return Ok(result); // 200 成功
        }

        // Google 第三方登入 API
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.IdToken))
            {
                return BadRequest(new ApiResponse<MemberLoginResponseDto>
                {
                    Success = false,
                    Message = "請提供有效的 Google 驗證憑證"
                });
            }
            // 調用 Service 進行驗證與自動註冊
            var serviceResult = await _memberService.LoginWithGoogleAsync(dto.IdToken);
            if (!serviceResult.Success)
            {
                return Unauthorized(serviceResult);
            }
            var loginDto = serviceResult.Data!;
            // 簽發本站 JWT Token 並賦予傳回的 DTO
            loginDto.Token = _jwtHelper.GenerateToken(loginDto.FId, loginDto.FName ?? "", loginDto.FEmail ?? "");
            return Ok(new ApiResponse<MemberLoginResponseDto>
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

        [Authorize]
        [HttpGet("test-header")]
        public IActionResult TestHeader()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            return Ok(new { header = authHeader });
        }

        //後台
        //會員列表
        [HttpGet("MemberList/list")]
        public async Task<IActionResult> GetMemberList()
        {
            var members = await _memberService.GetMemberListAsync();

            return Ok(new ApiResponse<List<MemberListDto>>
            {
                Success = true,
                Message = "成功取得會員列表",
                Data = members
            });
        }
        //黑名單列表
        [HttpGet("MemberList/blacklist")]
        public async Task<IActionResult> GetBlacklisted()
        {
            var resp = await _memberService.GetBlacklistedAsync();
            return Ok(resp);
        }

        // 解除封鎖會員
        [HttpPut("MemberList/unban/{id}")]
        public async Task<IActionResult> RemoveFromBlacklist(int id)
        {
            var result = await _memberService.RemoveFromBlacklistAsync(id);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }


}
