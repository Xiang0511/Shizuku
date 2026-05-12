using System.Collections.Generic;

namespace Shizuku.DTOs
{
    public class CreateOrderRequestDto
    {
        public int MemberId { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string ReceiverAddress { get; set; }
        public string Note { get; set; }
        public int PaymentMethodId { get; set; }
        public List<CartItemDto> CartItems { get; set; } = new List<CartItemDto>();
    }

    public class CartItemDto
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
