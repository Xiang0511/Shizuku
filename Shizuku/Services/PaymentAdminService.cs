using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shizuku.Models;

namespace Shizuku.Services
{
    // 後台金流行政管理服務
    // 職責：專責處理後台人員對全站金流交易流水、通訊日誌的查詢與統計資料庫操作
    // 解耦：將 Controller 與 Entity Framework 直接查詢解耦，提升代碼可重複使用性與測試性
    public class PaymentAdminService
    {
        private readonly DbShizukuDemoContext _db;

        public PaymentAdminService(DbShizukuDemoContext db)
        {
            _db = db;
        }

        // 取得全站所有金流交易明細流水
        public async Task<List<object>> GetTransactionsAsync()
        {
            var rawQuery = await _db.TPaymentTransactions
                .GroupJoin(_db.TOrders, pt => pt.FOrderId, o => o.FId, (pt, o) => new { pt, o })
                .SelectMany(x => x.o.DefaultIfEmpty(), (x, o) => new { x.pt, o })
                .GroupJoin(_db.TPaymentMethods, x => x.pt.FMethodId, pm => pm.FId, (x, pm) => new { x.pt, x.o, pm })
                .SelectMany(x => x.pm.DefaultIfEmpty(), (x, pm) => new
                {
                    x.pt.FId,
                    x.pt.FTransactionNo,
                    OrderNo = x.o != null ? x.o.FOrderNo : "未知訂單",
                    MethodName = pm != null ? pm.FMethodName : (x.pt.FMethodId == 1 ? "綠界金流" : (x.pt.FMethodId == 2 ? "LINE Pay" : (x.pt.FMethodId == 3 ? "貨到付款" : "未知付款方式"))),
                    x.pt.FAmount,
                    x.pt.FGatewayTradeNo,
                    x.pt.FStatus,
                    x.pt.FPaidAt,
                    x.pt.FCreatedAt
                })
                .OrderByDescending(x => x.FCreatedAt)
                .ToListAsync();

            return rawQuery.Cast<object>().ToList();
        }

        // 取得特定交易的詳細 API 通訊日誌
        public async Task<List<object>> GetTransactionLogsAsync(int transactionId)
        {
            var rawLogs = await _db.TPaymentLogs
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

            return rawLogs.Cast<object>().ToList();
        }
    }
}
