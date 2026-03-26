using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shizuku.Models.System
{
    [Table("SystemLogs")] // 確保名稱跟資料庫自動產生的表名一致
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }
        // 在 string 後面加上 ?，解決 SqlNullValueException
        public string? Message { get; set; }

        public string? MessageTemplate { get; set; }

        public string? Level { get; set; }

        // Timestamp 通常不為空，所以不用加
        public DateTime Timestamp { get; set; }

        public string? Exception { get; set; }

        public string? Properties { get; set; }
    }
}

