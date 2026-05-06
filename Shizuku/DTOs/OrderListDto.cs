using System;

namespace Shizuku.Models.DTOs
{
    public class OrderListDto
    {
        // 訂單編號 (對應前端的 id)
        public string OrderNo { get; set; } 

        // 訂單總金額 (對應前端的 total)
        public decimal TotalAmount { get; set; } 

        // 訂單狀態文字 (對應前端的 status，例如 "待處理", "已付款")
        public string StatusText { get; set; } 

        // 訂單建立時間 (對應前端的 date)
        public DateTime CreatedAt { get; set; } 
    }
}
