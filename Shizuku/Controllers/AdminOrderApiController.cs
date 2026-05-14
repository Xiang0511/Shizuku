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
    }

    // 用來接收前端傳來的新狀態數字 DTO (只有一行,為了方便就直接寫在這支檔案底下)
    public class UpdateOrderStatusDto
    {
        public int NewStatus { get; set; }
    }
}
