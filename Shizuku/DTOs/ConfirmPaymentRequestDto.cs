namespace Shizuku.DTOs
{
    public class ConfirmPaymentRequestDto
    {
        public string TransactionId { get; set; }
        public string OrderId { get; set; }
    }
}
