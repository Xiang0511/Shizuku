using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
using Shizuku.Services;
using System.Text.Json;

namespace Shizuku.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderApiController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly LinePayPaymentService _linePayPaymentService;
        private readonly ECPayPaymentService _ecPayPaymentService;

        // 建構子：移除 DbContext 與底層的 LinePayService，改依賴封裝好的服務
        public OrderApiController(
            OrderService orderService, 
            LinePayPaymentService linePayPaymentService, 
            ECPayPaymentService ecPayPaymentService)
        {
            _orderService = orderService;
            _linePayPaymentService = linePayPaymentService;
            _ecPayPaymentService = ecPayPaymentService;
        }

        // 建立訂單API /api/orderApi/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            var result = await _orderService.CreateOrder(request);
            return Ok(result);
        }

        // 確認付款api /api/orderApi/confirm
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequestDto request)
        {
            // 將確認扣款的細節交給 Service
            bool isSuccess = await _linePayPaymentService.ConfirmPaymentAsync(request.TransactionId, request.OrderId);
            
            if (isSuccess)
            {
                // 扣款成功，呼叫 OrderService 統一更新訂單狀態為「已付款」
                await _orderService.MarkOrderAsPaidAsync(request.OrderId);
                return Ok(new ApiResponse<object> { Success = true, Message = "付款成功！" });
            }
            
            return BadRequest(new ApiResponse<object> { Success = false, Message = "LINE Pay 扣款失敗！" });
        }

        // 讀取會員訂單列表API: /api/order/member/{memberId}  
        [HttpGet("member/{memberId}")]
        public async Task<IActionResult> GetMemberOrders(int memberId)
        {
            try
            {
                var orders = await _orderService.GetMemberOrdersAsync(memberId);
                return Ok(new ApiResponse<List<OrderListDto>>
                {
                    Success = true,
                    Message = "查詢訂單成功",
                    Data = orders
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "獲取訂單失敗：" + ex.Message
                });
            }
        }

        // 讀取訂單明細API /api/order/{orderNo}
        [HttpGet("{orderNo}")]
        public async Task<IActionResult> GetOrderDetail(string orderNo, [FromQuery] int memberId)
        {
            var result = await _orderService.GetOrderDetailAsync(orderNo, memberId);
            if (!result.Success) return NotFound(result); 
            return Ok(result);
        }

        // 生成綠界訂單表格API /api/orderApi/ecpay/{orderNo}
        [HttpGet("ecpay/{orderNo}")]
        public async Task<IActionResult> GenerateECPayForm(string orderNo)
        {
            string htmlForm = await _orderService.GenerateECPayHtmlFormAsync(orderNo);
            if (string.IsNullOrEmpty(htmlForm)) return NotFound(new ApiResponse<object> { Success = false, Message = "找不到這筆訂單" });
            return Content(htmlForm, "text/html");
        }

        // 綠界回傳API /api/orderApi/ecpayResult
        [HttpPost("ecpayResult")]
        public async Task<IActionResult> ECPayResult([FromForm] IFormCollection form)
        {
            try
            {
                // 將參數解析邏輯交給 ECPayPaymentService
                if (_ecPayPaymentService.ValidateECPayCallback(form, out string orderNo))
                {
                    // 驗證成功，呼叫 OrderService 統一更新訂單狀態
                    await _orderService.MarkOrderAsPaidAsync(orderNo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ECPay 狀態更新失敗：" + ex.Message);
            }

            string html = @"
         <html>
        <body style='display:flex; justify-content:center; align-items:center; height:100vh; font-family:sans-serif;'>
            <div style='text-align:center;'>
                <h2 style='color: #4CAF50;'>付款成功！</h2>
                <p>訂單已成立，此視窗將自動關閉，請返回原畫面...</p>
            </div>
            <script>
                if (window.opener) {
                    window.opener.postMessage('PAYMENT_SUCCESS', '*');
                }
                window.close();
            </script>
        </body>
        </html>";
            return Content(html, "text/html");
        }

        // 重新付款 /api/orderApi/repay/{orderNo}
        [HttpPost("repay/{orderNo}")]
        public async Task<IActionResult> RepayOrder(string orderNo, [FromBody] RepayRequestDto request)
        {
            // 透過 Service 撈取資料，取代原本的 _db.TOrders...
            var order = await _orderService.GetOrderAsync(orderNo);
            if (order == null) return NotFound(new ApiResponse<object> { Success = false, Message = "找不到該筆訂單" });

            if (order.FStatus != 1) return BadRequest(new ApiResponse<object> { Success = false, Message = "此訂單狀態無法重新付款" });

            try
            {
                // 如果切換為貨到付款(3)，直接更新訂單狀態為已付款
                if (request.PaymentMethodId == 3)
                {
                    await _orderService.MarkOrderAsPaidAsync(orderNo, request.PaymentMethodId);
                    return Ok(new ApiResponse<CreateOrderResponseDto>
                    {
                        Success = true,
                        Message = "付款方式已更改為貨到付款",
                        Data = new CreateOrderResponseDto
                        {
                            OrderNo = orderNo,
                            PaymentUrl = "" // 貨到付款沒有連結
                        }
                    });
                }

                string paymentUrl = await _orderService.GeneratePaymentUrlAsync(orderNo, request.PaymentMethodId, order.FTotalAmount);
                return Ok(new ApiResponse<CreateOrderResponseDto>
                {
                    Success = true,
                    Message = "產生付款連結成功",
                    Data = new CreateOrderResponseDto
                    {
                        OrderNo = orderNo,
                        PaymentUrl = paymentUrl
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "產生付款連結失敗：" + ex.Message });
            }
        }

        // 取消訂單 /api/orderApi/{orderNo}/cancel
        [HttpPatch("{orderNo}/cancel")]
        public async Task<IActionResult> CancelOrder(string orderNo)
        {
            var result = await _orderService.CancelOrderAsync(orderNo);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // 銷量報表API /api/orderApi/sales-stats
        [HttpGet("sales-stats")]
        public async Task<IActionResult> GetSalesStats()
        {
            try
            {
                var stats = await _orderService.GetSalesStatsAsync();
                return Ok(new ApiResponse<List<VariantSalesStatsDto>>
                {
                    Success = true,
                    Message = "查詢成功",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "系統錯誤: " + ex.Message });
            }
        }
    }
}
