namespace Shizuku.Models
{
    public class TProductStockRecord
    {
        public int FId { get; set; }
        public int FVariantId { get; set; }
        public string FType { get; set; } = string.Empty;
        public int FQuantity { get; set; }
        public decimal? FCostPrice { get; set; }
        public string? FNote { get; set; }
        public DateTime FCreatedAt { get; set; }
    }
}
