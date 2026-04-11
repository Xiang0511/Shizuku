namespace Shizuku.ViewModels
{
    public class OrderListViewModel
    {
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderDate { get; set; }
        public string Status { get; set; }
        public string PaymentMethod { get; set; }
    }
}