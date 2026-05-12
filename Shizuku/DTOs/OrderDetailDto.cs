namespace Shizuku.DTOs
{
    public class OrderDetailDto
    {
        //訂單主表資訊
        public string OrderNo { get; set; }
        public DateTime CreatedAt { get; set; }
        public string StatusText { get; set; }
        public decimal TotalAmount { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string ReceiverAddress { get; set; }
        public string Note { get; set; }
        public string PaymentMethod { get; set; } // 付款方式
        public decimal Subtotal { get; set; }     // 小計
        public decimal Discount { get; set; }     // 折扣
        public decimal ShippingFee { get; set; }  // 運費

        //訂單明細
        public List<OrderItemDto> Items { get; set; }
    }

    public class OrderItemDto
    {
        public string ProductName { get; set; }
        public string VariantName { get; set; } 
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string ProductImage { get; set; }
    }
}