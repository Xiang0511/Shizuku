public class TPurchaseOrder
{
    public int FId { get; set; }
    public string FOrderNo { get; set; } = string.Empty;
    public string? FSupplier { get; set; }
    public string? FPaymentMethod { get; set; }
    public string? FNote { get; set; }
    public int FTotalQuantity { get; set; }
    public decimal FTotalAmount { get; set; }
    public DateTime FCreatedAt { get; set; }
}