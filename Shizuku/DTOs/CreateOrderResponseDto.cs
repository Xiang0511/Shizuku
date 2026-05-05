namespace Shizuku.Models.DTOs
{
    public class CreateOrderResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string OrderNo { get; set; }
    }
}
