namespace Shizuku.DTOs
{
    // 專門用來接收前端傳來的「修改狀態」指令
    public class UpdateTicketStatusDto
    {
        public int TicketId { get; set; }    // 哪一封信？
        public string NewStatus { get; set; } // 要改成什麼狀態？
    }
}