using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
using Shizuku.Services;

namespace Shizuku.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderApiController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly PaymentFactory _paymentFactory;
        private readonly RefundAdminService _refundService;

        // 建構子注入：注入訂單服務、金流抽象工廠與退款服務
        public OrderApiController(OrderService orderService, PaymentFactory paymentFactory, RefundAdminService refundService)
        {
            _orderService = orderService;
            _paymentFactory = paymentFactory;
            _refundService = refundService;
        }

        // 建立新訂單 (POST /api/orderApi/create)
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            var result = await _orderService.CreateOrder(request);
            return Ok(result);
        }

        // 確認付款並扣款 (POST /api/orderApi/confirm)
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequestDto request)
        {
            try
            {
                // 由工廠取得 LINE Pay 服務進行扣款確認
                var linePayService = _paymentFactory.GetPaymentService(2) as LinePayPaymentService;
                if (linePayService == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "系統未正確配置 LINE Pay 金流服務"
                    });
                }

                bool isSuccess = await linePayService.ConfirmPaymentAsync(request.TransactionId, request.OrderId);
                if (isSuccess)
                {
                    await _orderService.MarkOrderAsPaidAsync(request.OrderId);
                    return Ok(new ApiResponse<object> { Success = true, Message = "付款成功！" });
                }

                return BadRequest(new ApiResponse<object> { Success = false, Message = "LINE Pay 扣款失敗！" });
            }
            catch (Exception ex)
            {
                return InternalServerError("LINE Pay 付款確認失敗", ex);
            }
        }

        // 讀取特定會員的所有訂單列表 (GET /api/orderApi/member/{memberId})
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
                return InternalServerError("獲取會員訂單列表失敗", ex);
            }
        }

        // 讀取特定訂單明細 (GET /api/orderApi/{orderNo})
        [HttpGet("{orderNo}")]
        public async Task<IActionResult> GetOrderDetail(string orderNo, [FromQuery] int memberId)
        {
            var result = await _orderService.GetOrderDetailAsync(orderNo, memberId);
            if (!result.Success) return NotFound(result); 
            return Ok(result);
        }

        // 產生自動轉向綠界收銀台的 HTML 表單 (GET /api/orderApi/ecpay/{orderNo})
        [HttpGet("ecpay/{orderNo}")]
        public async Task<IActionResult> GenerateECPayForm(string orderNo)
        {
            string htmlForm = await _orderService.GenerateECPayHtmlFormAsync(orderNo);
            if (string.IsNullOrEmpty(htmlForm))
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = "找不到這筆訂單" });
            }
            return Content(htmlForm, "text/html");
        }

        // 綠界金流非同步交易回傳通知 (POST /api/orderApi/ecpayResult)
        [HttpPost("ecpayResult")]
        public async Task<IActionResult> ECPayResult([FromForm] IFormCollection form)
        {
            try
            {
                var ecpayService = _paymentFactory.GetPaymentService(1) as ECPayPaymentService;
                if (ecpayService != null)
                {
                    var formDict = form.ToDictionary(k => k.Key, k => k.Value.ToString());
                    if (ecpayService.ValidateECPayCallback(formDict, out string orderNo))
                    {
                        await _orderService.MarkOrderAsPaidAsync(orderNo);
                    }
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

        // 重新發起付款流程 (POST /api/orderApi/repay/{orderNo})
        [HttpPost("repay/{orderNo}")]
        public async Task<IActionResult> RepayOrder(string orderNo, [FromBody] RepayRequestDto request)
        {
            var order = await _orderService.GetOrderAsync(orderNo);
            if (order == null)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = "找不到該筆訂單" });
            }

            if (order.FStatus != 1)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "此訂單狀態無法重新付款" });
            }

            try
            {
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
                            PaymentUrl = ""
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
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"產生付款連結失敗: {ex.Message}"
                });
            }
        }

        // 會員手動取消未付款訂單 (HttpPatch /api/orderApi/{orderNo}/cancel)
        [HttpPatch("{orderNo}/cancel")]
        public async Task<IActionResult> CancelOrder(string orderNo)
        {
            var result = await _orderService.CancelOrderAsync(orderNo);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // 取得前台統計用銷量數據 (GET /api/orderApi/sales-stats)
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
                return InternalServerError("查詢銷量統計失敗", ex);
            }
        }

        // 前台會員申請退款 (POST /api/OrderApi/{orderNo}/refund)
        [HttpPost("{orderNo}/refund")]
        public async Task<IActionResult> RequestRefund(string orderNo, [FromBody] RefundRequestDto request)
        {
            var result = await _refundService.RequestRefundAsync(orderNo, request.Reason);
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
