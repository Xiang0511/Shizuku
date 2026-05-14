using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Shizuku.Models
{
    public partial class TLiveChatMessage
    {
        public int FId { get; set; }

        [DisplayName("會員ID")]
        public int FMemberId { get; set; }

        [DisplayName("發送者身分")]
        public string FSenderType { get; set; } = null!;

        [DisplayName("發送者姓名")]
        public string FSenderName { get; set; } = null!;

        [DisplayName("訊息內容")]
        public string FMessage { get; set; } = null!;

        [DisplayName("發送時間")]
        public DateTime FSendTime { get; set; }
    }
}