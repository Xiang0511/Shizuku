public class TPurchaseOrder
{
    public int FId { get; set; }
    public string FOrderNo { get; set; } = string.Empty;
    public string? FSupplier { get; set; }
    public string? FPaymentMethod { get; set; }
    public string FType { get; set; } = "進貨";      // ← 加
    public string FStatus { get; set; } = "已完成";    // ← 加
    public string? FInvoiceNo { get; set; }                // ← 加
    public DateTime? FInvoiceDate { get; set; }                // ← 加
    public string FTaxType { get; set; } = "應稅";      // ← 加
    public decimal FTaxRate { get; set; } = 5.00m;       // ← 加
    public decimal FUntaxedAmount { get; set; }                // ← 加
    public decimal FTaxAmount { get; set; }                // ← 加
    public string? FNote { get; set; }
    public int FTotalQuantity { get; set; }
    public decimal FTotalAmount { get; set; }
    public DateTime FCreatedAt { get; set; }
}