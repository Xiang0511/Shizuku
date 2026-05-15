using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;

[ApiController]
[Route("api/[controller]")]
public class VerificationApiController : ControllerBase
{
    private readonly VerificationService _verificationService;
    private readonly EmailService _emailService;

    public VerificationApiController(VerificationService verificationService,EmailService emailService)
    {
        _verificationService = verificationService;
        _emailService = emailService;
    }

    /// <summary>
    /// 會員點擊 Email 連結後呼叫的 API
    /// </summary>
    [HttpGet("confirm")]
    public async Task<ApiResponse<bool>> Confirm(string token)
    {
        try
        {
            // 呼叫 Service 執行邏輯
            var result = await _verificationService.VerifyEmailTokenAsync(token);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "驗證成功！歡迎加入 Shizuku。",
                Data = result
            };
        }
        catch (Exception ex)
        {
            // 捕捉 Service 丟出來的各種錯誤訊息
            return new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Data = false
            };
        }
    }
    [HttpPost("test-send")]
    public async Task<ApiResponse<bool>> TestSend(string targetEmail)
    {
        try
        {
            // 這裡直接調用你的 EmailService
            await _emailService.SendEmailAsync(
                targetEmail,
                "Shizuku 測試信件",
                "<h1>看到這封信代表你的 SMTP 設定成功了！</h1>"
            );

            return new ApiResponse<bool> { Success = true, Message = "發送成功", Data = true };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { Success = false, Message = ex.Message, Data = false };
        }
    }
}