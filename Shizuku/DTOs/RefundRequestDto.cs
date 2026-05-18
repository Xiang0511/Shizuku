namespace Shizuku.DTOs
{
    // 前台會員申請退款 DTO
    public class RefundRequestDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    // 後台駁回退款 DTO
    public class RejectRefundDto
    {
        public string Reason { get; set; } = string.Empty;
    }
}
