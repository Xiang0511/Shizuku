using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
using Shizuku.Services;

namespace Shizuku.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminOrderApiController : ControllerBase
    {
        private readonly OrderAdminService _orderAdminService;
        private readonly AnomalyMonitorService _anomalyMonitorService;
        private readonly PaymentAnomalyService _paymentAnomalyService;
        private readonly OrderAnomalyService _orderAnomalyService;
        private readonly RefundAdminService _refundService;

        // 建構子注入
        public AdminOrderApiController(
            OrderAdminService orderAdminService,
            AnomalyMonitorService anomalyMonitorService,
            PaymentAnomalyService paymentAnomalyService,
            OrderAnomalyService orderAnomalyService,
            RefundAdminService refundService)
        {
            _orderAdminService = orderAdminService;
            _anomalyMonitorService = anomalyMonitorService;
            _paymentAnomalyService = paymentAnomalyService;
            _orderAnomalyService = orderAnomalyService;
            _refundService = refundService;
        }

        // 取得全站所有訂單 (GET /api/AdminOrderApi)
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _orderAdminService.GetAllOrdersForAdminAsync();
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "查詢全站訂單成功",
                    Data = orders
                });
            }
            catch (Exception ex)
            {
                return InternalServerError("查詢全站訂單失敗", ex);
            }
        }

        // 取得特定訂單明細 (GET /api/AdminOrderApi/{orderNo})
        [HttpGet("{orderNo}")]
        public async Task<IActionResult> GetOrderDetail(string orderNo)
        {
            var result = await _orderAdminService.GetOrderDetailForAdminAsync(orderNo);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        // 修改訂單狀態 (PATCH /api/AdminOrderApi/{orderNo}/status)
        [HttpPatch("{orderNo}/status")]
        public async Task<IActionResult> UpdateOrderStatus(string orderNo, [FromBody] UpdateOrderStatusDto request)
        {
            var result = await _orderAdminService.UpdateOrderStatusAsync(orderNo, request.NewStatus);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // 強制取消訂單 (PATCH /api/AdminOrderApi/{orderNo}/cancel)
        [HttpPatch("{orderNo}/cancel")]
        public async Task<IActionResult> CancelOrder(string orderNo)
        {
            var result = await _orderAdminService.CancelOrderForAdminAsync(orderNo);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // 取得全站異常監控清單 (GET /api/AdminOrderApi/abnormal)
        [HttpGet("abnormal")]
        public async Task<IActionResult> GetAbnormalOrders()
        {
            try
            {
                var abnormals = await _anomalyMonitorService.GetAbnormalOrdersAsync();
                return Ok(new ApiResponse<List<AbnormalOrderDto>>
                {
                    Success = true,
                    Message = "獲取異常監控清單成功",
                    Data = abnormals
                });
            }
            catch (Exception ex)
            {
                return InternalServerError("掃描異常訂單失敗", ex);
            }
        }

        // 執行訂單救援 (POST /api/AdminOrderApi/{orderNo}/rescue)
        [HttpPost("{orderNo}/rescue")]
        public async Task<IActionResult> RescueOrder(string orderNo)
        {
            var result = await _anomalyMonitorService.RescueOrderAsync(orderNo);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // 取得出貨中心訂單清單 (GET /api/AdminOrderApi/shipping)
        [HttpGet("shipping")]
        public async Task<IActionResult> GetShippingOrders([FromQuery] int status)
        {
            var orders = await _orderAdminService.GetShippingOrdersAsync(status);
            return Ok(new ApiResponse<object> { Success = true, Message = "獲取出貨清單成功", Data = orders });
        }

        // 批次更新訂單狀態 (POST /api/AdminOrderApi/batch-status)
        [HttpPost("batch-status")]
        public async Task<IActionResult> BatchUpdateStatus([FromBody] BatchUpdateStatusDto request)
        {
            var result = await _orderAdminService.BatchUpdateOrderStatusAsync(request.OrderNos, request.NewStatus);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // 取得營收統計數據 (GET /api/AdminOrderApi/revenue-stats)
        [HttpGet("revenue-stats")]
        public async Task<IActionResult> GetRevenueStats([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var stats = await _orderAdminService.GetRevenueStatsAsync(startDate, endDate);
                return Ok(new ApiResponse<object> { Success = true, Message = "獲取營收統計成功", Data = stats });
            }
            catch (Exception ex)
            {
                return InternalServerError("獲取營收統計失敗", ex);
            }
        }

        // 取得異常支付資料清單 (GET /api/AdminOrderApi/payment-anomalies)
        [HttpGet("payment-anomalies")]
        public async Task<IActionResult> GetPaymentAnomalies()
        {
            try
            {
                var tenMinutesAgo = DateTime.Now.AddMinutes(-10);
                var highFreqFailures = await _anomalyMonitorService.GetHighFreqFailuresAsync(tenMinutesAgo);
                var highAmountTxns = await _anomalyMonitorService.GetHighAmountTxnsAsync(tenMinutesAgo);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "獲取異常支付資料成功",
                    Data = new { highFreqFailures, highAmountTxns }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError("獲取異常支付資料失敗", ex);
            }
        }

        // 手動觸發金流異常掃描 (POST /api/AdminOrderApi/trigger-payment-scan)
        [HttpPost("trigger-payment-scan")]
        public async Task<IActionResult> TriggerPaymentScan()
        {
            await _paymentAnomalyService.ScanAsync();
            return Ok(new ApiResponse<object> { Success = true, Message = "金流異常掃描已執行，如有偵測到異常將立即推播" });
        }

        // 手動觸發訂單異常掃描 (POST /api/AdminOrderApi/trigger-order-scan)
        [HttpPost("trigger-order-scan")]
        public async Task<IActionResult> TriggerOrderScan()
        {
            await _orderAnomalyService.ScanAsync();
            return Ok(new ApiResponse<object> { Success = true, Message = "訂單異常掃描已執行，如有偵測到異常將立即推播" });
        }

        // 取得待退款訂單列表 (GET /api/AdminOrderApi/refunds)
        [HttpGet("refunds")]
        public async Task<IActionResult> GetPendingRefunds()
        {
            try
            {
                var orders = await _refundService.GetPendingRefundOrdersAsync();
                return Ok(new ApiResponse<object> { Success = true, Message = "取得待退款訂單成功", Data = orders });
            }
            catch (Exception ex)
            {
                return InternalServerError("查詢待退款訂單失敗", ex);
            }
        }

        // 核准退款 (POST /api/AdminOrderApi/{orderNo}/approve-refund)
        [HttpPost("{orderNo}/approve-refund")]
        public async Task<IActionResult> ApproveRefund(string orderNo)
        {
            var result = await _refundService.ApproveRefundAsync(orderNo);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // 駁回退款申請 (POST /api/AdminOrderApi/{orderNo}/reject-refund)
        [HttpPost("{orderNo}/reject-refund")]
        public async Task<IActionResult> RejectRefund(string orderNo, [FromBody] RejectRefundDto request)
        {
            var result = await _refundService.RejectRefundAsync(orderNo, request.Reason);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // 輔助方法：統一處理錯誤訊息與狀態碼回傳
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
