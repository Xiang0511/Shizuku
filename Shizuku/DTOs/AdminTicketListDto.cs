namespace Shizuku.DTOs
{
    // 專門給「後台管理員」看的表單紀錄格式
    public class AdminTicketListDto
    {
        public int Id { get; set; }
        public int MemberId { get; set; }

        // 後台需要知道發問人是誰、信箱是什麼
        public string GuestName { get; set; }
        public string Email { get; set; }

        public string Category { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public string Status { get; set; }
        public string CreateTime { get; set; }
    }
}