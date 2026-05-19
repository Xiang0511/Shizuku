namespace Shizuku.DTOs
{
    public class AbnormalOrderDto
    {
        public string OrderNo { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        // 異常診斷資訊
        public string AbnormalityType { get; set; } = string.Empty; // Conflict, Security, Behavior
        public string Description { get; set; } = string.Empty;
        public int? RelatedCount { get; set; }                     // 相關次數 (如嘗試失敗次數)
        public string Suggestion { get; set; } = string.Empty;
    }

    public class RescueOrderResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string NewStatusText { get; set; } = string.Empty;
    }
}
