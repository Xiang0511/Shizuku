namespace Shizuku.DTOs
{
    public class VariantSalesStatsDto
    {
        public int VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty; // ←13 加
        public string ProductCode { get; set; } = string.Empty; // ←13 加
         public string Color { get; set; } = string.Empty; // ←13 加
         public string Size { get; set; } = string.Empty;  // ←13 加
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
