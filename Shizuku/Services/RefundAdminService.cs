using Microsoft.EntityFrameworkCore;
using Shizuku.DTOs;
using Shizuku.Enums;
using Shizuku.Models;

namespace Shizuku.Services
{
    // 退款管理中心核心服務
    // 負責退款申請、核准退款（含金流 API 呼叫）、駁回退款三大流程
    // 所有狀態流轉與庫存回補均在 DbTransaction 保護下安全執行
    public class RefundAdminService
    {
        private readonly DbShizukuDemoContext _db;
        private readonly PaymentFactory _paymentFactory;
        private readonly ProductService _productService;

        public RefundAdminService(
            DbShizukuDemoContext db,
            PaymentFactory paymentFactory,
            ProductService productService)
        {
            _db = db;
            _paymentFactory = paymentFactory;
            _productService = productService;
        }

        // ==============================
        // 1. 前台會員申請退款 (狀態 4 → 6 或狀態 2 → 7 直奔秒退款)
        // ==============================
        public async Task<ApiResponse<object>> RequestRefundAsync(string orderNo, string reason)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null)
                return new ApiResponse<object> { Success = false, Message = "找不到該筆訂單" };

            // 【智慧秒退款】如果是「已付款」且「尚未出貨」的狀態，允許自助秒退款
            if (order.FStatus == (int)OrderStatus.Paid)
            {
                // 先將狀態標記為待退款，並記錄秒退原因，以便直接複用後台核准 API
                order.FStatus = (int)OrderStatus.PendingRefund;
                order.FNote = string.IsNullOrEmpty(order.FNote)
                    ? $"[自助秒退] 退款原因：{reason}"
                    : $"{order.FNote}\n[自助秒退] 退款原因：{reason}";
                await _db.SaveChangesAsync();

                // 直接調用後台核准 API，完成金流退刷與庫存安全回補
                var refundResult = await ApproveRefundAsync(orderNo);
                if (refundResult.Success)
                {
                    refundResult.Message = "退款已秒速完成！金流已自動退刷，且訂單金額已全額返還。";
                }
                else
                {
                    // 萬一秒退失敗（例如金流商 API 異常），提醒會員已進入人工程序，且後台會自動看見此單
                    refundResult.Message = $"自動退刷暫時無法完成，已為您轉入客服人工處理程序。原因：{refundResult.Message}";
                }
                return refundResult;
            }

            // 【常規退款】如果已經出貨或送達，則走常規的客服審核流程
            if (order.FStatus != (int)OrderStatus.Delivered)
                return new ApiResponse<object> { Success = false, Message = "此訂單目前狀態不支援申請退款。" };

            order.FStatus = (int)OrderStatus.PendingRefund;
            order.FUpdatedAt = DateTime.Now;
            order.FNote = string.IsNullOrEmpty(order.FNote)
                ? $"退款原因：{reason}"
                : $"{order.FNote}\n退款原因：{reason}";

            await _db.SaveChangesAsync();

            return new ApiResponse<object> { Success = true, Message = "退款申請已提交，請等待客服審核。" };
        }

        // ==============================
        // 2. 後台核准退款 (狀態 6 → 7)
        // ==============================
        public async Task<ApiResponse<object>> ApproveRefundAsync(string orderNo)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null)
                return new ApiResponse<object> { Success = false, Message = "找不到該筆訂單" };

            if (order.FStatus != (int)OrderStatus.PendingRefund)
                return new ApiResponse<object> { Success = false, Message = "此訂單不在待退款狀態，無法核准退款。" };

            // 取得金流交易紀錄
            var paymentTx = await _db.TPaymentTransactions
                .Where(pt => pt.FOrderId == order.FId)
                .OrderByDescending(pt => pt.FCreatedAt)
                .FirstOrDefaultAsync();

            if (paymentTx == null)
                return new ApiResponse<object> { Success = false, Message = "找不到相關金流交易紀錄" };

            // 依金流管道呼叫對應的退款 API（LINE Pay 為真實退款，ECPay/COD 為模擬退款）
            var paymentService = _paymentFactory.GetPaymentService(paymentTx.FMethodId);
            var refundResult = await paymentService.RefundAsync(
                orderNo,
                paymentTx.FAmount,
                paymentTx.FGatewayTradeNo ?? ""
            );

            if (!refundResult.Success)
                return refundResult; // 金流退款失敗，直接回傳錯誤

            // 金流退款成功後，啟動 DbTransaction 進行狀態流轉 + 庫存回補
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    // 更新訂單狀態為「已退款 (7)」
                    order.FStatus = (int)OrderStatus.Refunded;
                    order.FUpdatedAt = DateTime.Now;

                    // 更新金流交易狀態為「已退款 (3)」
                    paymentTx.FStatus = (int)PaymentStatus.Refunded;

                    // 安全回補商品規格庫存
                    var details = await _db.TOrderDetails
                        .Where(d => d.FOrderId == order.FId)
                        .ToListAsync();

                    foreach (var item in details)
                    {
                        bool restored = await _productService.RestoreStockAsync(item.FVariantId, item.FQuantity);
                        if (!restored)
                            throw new Exception($"回補庫存失敗，規格 ID: {item.FVariantId}");
                    }

                    await _db.SaveChangesAsync();
                    transaction.Commit();

                    return new ApiResponse<object>
                    {
                        Success = true,
                        Message = $"退款成功！訂單 {orderNo} 已退款，庫存已安全回補。"
                    };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"退款狀態更新失敗：{ex.Message}"
                    };
                }
            }
        }

        // ==============================
        // 3. 後台駁回退款申請 (狀態 6 → 4)
        // ==============================
        public async Task<ApiResponse<object>> RejectRefundAsync(string orderNo, string rejectReason)
        {
            var order = await _db.TOrders.FirstOrDefaultAsync(o => o.FOrderNo == orderNo);
            if (order == null)
                return new ApiResponse<object> { Success = false, Message = "找不到該筆訂單" };

            if (order.FStatus != (int)OrderStatus.PendingRefund)
                return new ApiResponse<object> { Success = false, Message = "此訂單不在待退款狀態，無法駁回。" };

            order.FStatus = (int)OrderStatus.Delivered; // 退回已送達狀態
            order.FUpdatedAt = DateTime.Now;
            order.FNote = string.IsNullOrEmpty(order.FNote)
                ? $"駁回原因：{rejectReason}"
                : $"{order.FNote}\n駁回原因：{rejectReason}";

            await _db.SaveChangesAsync();

            return new ApiResponse<object> { Success = true, Message = "已駁回該筆退款申請。" };
        }

        // ==============================
        // 4. 取得所有待退款訂單列表 (供後台管理面板使用)
        // ==============================
        public async Task<List<object>> GetPendingRefundOrdersAsync()
        {
            var orders = await _db.TOrders
                .Where(o => o.FStatus == (int)OrderStatus.PendingRefund)
                .OrderBy(o => o.FUpdatedAt) // FIFO 先進先出
                .ToListAsync();

            var result = new List<object>();

            foreach (var o in orders)
            {
                // 取得金流交易紀錄以判斷付款管道
                var paymentTx = await _db.TPaymentTransactions
                    .Where(pt => pt.FOrderId == o.FId)
                    .OrderByDescending(pt => pt.FCreatedAt)
                    .FirstOrDefaultAsync();

                string methodName = "未知";
                if (paymentTx != null)
                {
                    var method = await _db.TPaymentMethods.FirstOrDefaultAsync(m => m.FId == paymentTx.FMethodId);
                    methodName = method != null ? method.FMethodName : (paymentTx.FMethodId == 1 ? "綠界金流" : (paymentTx.FMethodId == 2 ? "LINE Pay" : (paymentTx.FMethodId == 3 ? "貨到付款" : "未知")));
                }

                // 取得會員名稱
                var member = await _db.TMembers.FirstOrDefaultAsync(m => m.FId == o.FMemberId);

                result.Add(new
                {
                    orderNo = o.FOrderNo,
                    memberName = member?.FName ?? "未知會員",
                    totalAmount = o.FTotalAmount,
                    paymentMethod = methodName,
                    note = o.FNote,
                    createdAt = o.FCreatedAt,
                    updatedAt = o.FUpdatedAt
                });
            }

            return result;
        }
    }
}
