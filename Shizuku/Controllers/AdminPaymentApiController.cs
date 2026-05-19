using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
using Shizuku.Services;

namespace Shizuku.Controllers
{
    [Route("api/admin/payments")]
    [ApiController]
    public class AdminPaymentApiController : ControllerBase
    {
        private readonly PaymentAdminService _paymentAdminService;

        // 構造子注入：消除對資料庫實體的直接依賴，改為注入行政管理服務，符合 SoC 與高解耦性
        public AdminPaymentApiController(PaymentAdminService paymentAdminService)
        {
            _paymentAdminService = paymentAdminService;
        }

        // 取得所有金流交易列表 (GET /api/admin/payments)
        [HttpGet]
        public async Task<IActionResult> GetTransactions()
        {
            try
            {
                var transactions = await _paymentAdminService.GetTransactionsAsync();
                return Ok(new ApiResponse<object> 
                { 
                    Success = true, 
                    Message = "取得金流交易列表成功", 
                    Data = transactions 
                });
            }
            catch (Exception ex)
            {
                return InternalServerError("取得金流交易列表失敗", ex);
            }
        }

        // 取得特定交易的詳細通訊日誌 (GET /api/admin/payments/{transactionId}/logs)
        [HttpGet("{transactionId}/logs")]
        public async Task<IActionResult> GetLogs(int transactionId)
        {
            try
            {
                var logs = await _paymentAdminService.GetTransactionLogsAsync(transactionId);
                return Ok(new ApiResponse<object> 
                { 
                    Success = true, 
                    Message = "取得金流交易詳細日誌成功", 
                    Data = logs 
                });
            }
            catch (Exception ex)
            {
                return InternalServerError("取得金流交易詳細日誌失敗", ex);
            }
        }

        // 輔助方法：統一的伺服器內部錯誤回應，避免程式碼重複，符合 SRP
        private IActionResult InternalServerError(string customMessage, Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"{customMessage}: {ex.Message}"
            });
        }
    }
}
