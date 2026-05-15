using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;

[ApiController]
[Route("api/[controller]")]
public class VerificationApiController : ControllerBase
{
    private readonly VerificationService _verificationService;
    private readonly EmailService _emailService;

    public VerificationApiController(VerificationService verificationService, EmailService emailService)
    {
        _verificationService = verificationService;
        _emailService = emailService;
    }

    /// <summary>
    /// 會員輸入 6 位數驗證碼後呼叫的 API
    /// </summary>
    [HttpPost("verify-code")] // 改為 Post，對應前端輸入
    public async Task<ApiResponse<bool>> VerifyCode([FromBody] VerifyRequestDto dto)
    {
        try
        {
            // 這裡 Code 一定要有值
            if (string.IsNullOrEmpty(dto.Code))
            {
                return new ApiResponse<bool> { Success = false, Message = "請輸入驗證碼", Data = false };
            }

            // 呼叫 Service 的新方法：VerifyCodeAsync
            var result = await _verificationService.VerifyCodeAsync(dto.MemberId, dto.Code);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "驗證成功！歡迎加入 Shizuku。",
                Data = result
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Data = false
            };
        }
    }

    // --- 修改後的測試流程 ---
    [HttpPost("test-send-code")]
    public async Task<ApiResponse<string>> TestSendCode(int memberId, string targetEmail)
    {
        try
        {
            // 1. 產生 6 位數代碼
            string code = await _verificationService.CreateEmailVerificationAsync(memberId);

            // 2. 寄信內容改為顯示數字
            string content = $@"
                <div style='text-align:center; padding:20px; border:1px solid #ddd;'>
                    <h2>Shizuku 驗證碼</h2>
                    <p>您的驗證碼如下，請於 10 分鐘內輸入：</p>
                    <h1 style='color:#3b82f6; font-size:40px; letter-spacing:10px;'>{code}</h1>
                </div>";

            await _emailService.SendEmailAsync(targetEmail, "Shizuku 驗證碼測試", content);

            return new ApiResponse<string> { Success = true, Message = "驗證碼已寄出", Data = code };
        }
        catch (Exception ex)
        {
            return new ApiResponse<string> { Success = false, Message = ex.Message, Data = null };
        }
    }
}