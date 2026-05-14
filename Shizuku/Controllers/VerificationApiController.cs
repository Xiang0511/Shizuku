using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;

[ApiController]
[Route("api/[controller]")]
public class VerificationApiController : ControllerBase
{
    private readonly VerificationService _verificationService;

    public VerificationApiController(VerificationService verificationService)
    {
        _verificationService = verificationService;
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
}