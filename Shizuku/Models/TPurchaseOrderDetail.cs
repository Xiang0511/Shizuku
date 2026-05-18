public class TPurchaseOrderDetail
{
    public int FId { get; set; }
    public int FOrderId { get; set; }
    public int FVariantId { get; set; }
    public int FQuantity { get; set; }
    public decimal? FCostPrice { get; set; }
    public decimal? FAmount { get; set; }
    public string? FNote { get; set; }
}