namespace Shizuku.DTOs
{
    // 專門接收 Vue 前台傳來的表單資料
    public class VueTicketDto
    {

        public string? Name { get; set; }
        public string? Email { get; set; }
        public int CategoryId { get; set; }
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public int MemberId { get; set; }
    }
}