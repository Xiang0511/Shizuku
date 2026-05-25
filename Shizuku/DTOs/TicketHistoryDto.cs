namespace Shizuku.DTOs
{
    public class TicketHistoryDto
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public string CreateTime { get; set; }
        public int Status { get; set; } // 0: 處理中, 1: 已結案
    }
}