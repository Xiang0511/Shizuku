using System.Collections.Generic;

namespace Shizuku.DTOs
{
    // 批次更新訂單狀態 DTO
    public class BatchUpdateStatusDto
    {
        public List<string> OrderNos { get; set; } = new();
        public int NewStatus { get; set; }
    }
}
