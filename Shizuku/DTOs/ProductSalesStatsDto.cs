namespace Shizuku.DTOs
{
    public class ProductSalesStatsDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public bool IsHot { get; set; }
        public bool IsNew { get; set; }
    }
}
