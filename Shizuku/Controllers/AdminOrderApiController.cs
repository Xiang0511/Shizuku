using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
using Shizuku.Services;

namespace Shizuku.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminOrderApiController : ControllerBase
    {
        private readonly OrderService _orderService;

        public AdminOrderApiController(OrderService orderService)
        {
            _orderService = orderService;
        }

        //  取得全站所有訂單 (GET /api/AdminOrderApi)
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersForAdminAsync();
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "查詢全站訂單成功",
                    Data = orders
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "系統錯誤: " + ex.Message
                });
            }
        }

        //  取得特定訂單明細 (GET /api/AdminOrderApi/{orderNo})
        [HttpGet("{orderNo}")]
        public async Task<IActionResult> GetOrderDetail(string orderNo)
        {
            var result = await _orderService.GetOrderDetailForAdminAsync(orderNo);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        //  修改訂單狀態 (PATCH /api/AdminOrderApi/{orderNo}/status)
        [HttpPatch("{orderNo}/status")]
        public async Task<IActionResult> UpdateOrderStatus(string orderNo, [FromBody] UpdateOrderStatusDto request)
        {
            var result = await _orderService.UpdateOrderStatusAsync(orderNo, request.NewStatus);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        //強制取消訂單 (PATCH /api/AdminOrderApi/{orderNo}/cancel)
        [HttpPatch("{orderNo}/cancel")]
        public async Task<IActionResult> CancelOrder(string orderNo)
        {
            var result = await _orderService.CancelOrderForAdminAsync(orderNo);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
        //  取得全站異常監控清單 (GET /api/AdminOrderApi/abnormal)
        [HttpGet("abnormal")]
        public async Task<IActionResult> GetAbnormalOrders()
        {
            try
            {
                var abnormals = await _orderService.GetAbnormalOrdersAsync();
                return Ok(new ApiResponse<List<AbnormalOrderDto>>
                {
                    Success = true,
                    Message = "獲取異常監控清單成功",
                    Data = abnormals
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "掃描異常訂單失敗: " + ex.Message });
            }
        }

        //  執行訂單救援 (POST /api/AdminOrderApi/{orderNo}/rescue)
        [HttpPost("{orderNo}/rescue")]
        public async Task<IActionResult> RescueOrder(string orderNo)
        {
            var result = await _orderService.RescueOrderAsync(orderNo);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
        //  取得出貨中心訂單清單 (GET /api/AdminOrderApi/shipping?status=2)
        [HttpGet("shipping")]
        public async Task<IActionResult> GetShippingOrders([FromQuery] int status)
        {
            var orders = await _orderService.GetShippingOrdersAsync(status);
            return Ok(new ApiResponse<object> { Success = true, Message = "獲取出貨清單成功", Data = orders });
        }

        //  批次更新訂單狀態 (POST /api/AdminOrderApi/batch-status)
        [HttpPost("batch-status")]
        public async Task<IActionResult> BatchUpdateStatus([FromBody] BatchUpdateStatusDto request)
        {
            var result = await _orderService.BatchUpdateOrderStatusAsync(request.OrderNos, request.NewStatus);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
        //  取得營收統計數據 (GET /api/AdminOrderApi/revenue-stats?startDate=2023-01-01&endDate=2023-01-31)
        [HttpGet("revenue-stats")]
        public async Task<IActionResult> GetRevenueStats([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var stats = await _orderService.GetRevenueStatsAsync(startDate, endDate);
            return Ok(new ApiResponse<object> { Success = true, Message = "獲取營收統計成功", Data = stats });
        }

        //  取得異常支付資料清單 (GET /api/AdminOrderApi/payment-anomalies)
        //  供 AnomalyPaymentWidget 顯示高頻失敗與異常高額交易
        [HttpGet("payment-anomalies")]
        public async Task<IActionResult> GetPaymentAnomalies()
        {
            try
            {
                var tenMinutesAgo = DateTime.Now.AddMinutes(-10);

                // 高頻失敗清單
                var highFreqFailures = await _orderService.GetHighFreqFailuresAsync(tenMinutesAgo);

                // 異常高額交易清單
                var highAmountTxns = await _orderService.GetHighAmountTxnsAsync(tenMinutesAgo);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "獲取異常支付資料成功",
                    Data = new { highFreqFailures, highAmountTxns }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "系統錯誤: " + ex.Message });
            }
        }

        // 手動觸發金流異常掃描 (POST /api/AdminOrderApi/trigger-payment-scan)
        // 職責：呼叫真實偵測邏輯，並透過 SignalR 推播至金流控制中心，用於驗證通知系統
        [HttpPost("trigger-payment-scan")]
        public async Task<IActionResult> TriggerPaymentScan(
            [FromServices] PaymentAnomalyService paymentAnomalyService)
        {
            await paymentAnomalyService.ScanAsync();
            return Ok(new ApiResponse<object> { Success = true, Message = "金流異常掃描已執行，如有偵測到異常將立即推播" });
        }

        // 手動觸發訂單異常掃描 (POST /api/AdminOrderApi/trigger-order-scan)
        // 職責：呼叫真實偵測邏輯，並透過 SignalR 推播至訂單控制中心，用於驗證通知系統
        [HttpPost("trigger-order-scan")]
        public async Task<IActionResult> TriggerOrderScan(
            [FromServices] OrderAnomalyService orderAnomalyService)
        {
            await orderAnomalyService.ScanAsync();
            return Ok(new ApiResponse<object> { Success = true, Message = "訂單異常掃描已執行，如有偵測到異常將立即推播" });
        }
    }
    //TODO:待處理
    public class BatchUpdateStatusDto
    {
        public List<string> OrderNos { get; set; } = new();
        public int NewStatus { get; set; }
    }

    // 用來接收前端傳來的新狀態數字 DTO (只有一行,為了方便就直接寫在這支檔案底下)
    public class UpdateOrderStatusDto
    {
        public int NewStatus { get; set; }
    }
}
