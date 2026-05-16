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
    }

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
