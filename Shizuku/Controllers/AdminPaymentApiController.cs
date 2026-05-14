using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shizuku.Models;
using Shizuku.DTOs;

namespace Shizuku.Controllers
{
    [Route("api/admin/payments")]
    [ApiController]
    public class AdminPaymentApiController : ControllerBase
    {
        private readonly DbShizukuDemoContext _db;

        public AdminPaymentApiController(DbShizukuDemoContext db)
        {
            _db = db;
        }

        //取得所有金流交易列表
        [HttpGet]
        public async Task<IActionResult> GetTransactions()
        {
            var query = await _db.TPaymentTransactions
            .GroupJoin(_db.TOrders, pt => pt.FOrderId, o => o.FId, (pt, o) => new { pt, o })
            .SelectMany(x => x.o.DefaultIfEmpty(), (x, o) => new { x.pt, o })
            .GroupJoin(_db.TPaymentMethods, x => x.pt.FMethodId, pm => pm.FId, (x, pm) => new { x.pt, x.o, pm })
            .SelectMany(x => x.pm.DefaultIfEmpty(), (x, pm) => new
            {
                x.pt.FId,
                x.pt.FTransactionNo,
                OrderNo = x.o != null ? x.o.FOrderNo : "未知訂單",
                MethodName = pm != null ? pm.FMethodName : "未知付款方式",
                x.pt.FAmount,
                x.pt.FGatewayTradeNo,
                x.pt.FStatus,
                x.pt.FPaidAt,
                x.pt.FCreatedAt
            })
            .OrderByDescending(x => x.FCreatedAt)
            .ToListAsync();
            return Ok(new ApiResponse<object> { Success = true, Message = "取得列表成功", Data = query });
        }

        //取得特定交易的詳細日誌
        [HttpGet("{transactionId}/logs")]
        public async Task<IActionResult> GetLogs(int transactionId)
        {
            var logs = await _db.TPaymentLogs
                .Where(l => l.FPaymentTransactionsId == transactionId)
                .OrderBy(l => l.FCreatedAt)
                .Select(l => new
                {
                    l.FActionType,
                    l.FRequestData,
                    l.FResponseData,
                    l.FCreatedAt
                })
                .ToListAsync();

            return Ok(new ApiResponse<object> 
            { 
                Success = true, 
                Message = "取得通訊日誌成功", 
                Data = logs 
            });
        }
    }
}
