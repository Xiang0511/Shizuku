namespace Shizuku.DTOs
{
    public class ProductCheckDto
    {
        public int VariantId { get; set; }
        public decimal LatestPrice { get; set; }
        public int CurrentStock { get; set; }
        public string ProductName { get; set; }
    }
}
