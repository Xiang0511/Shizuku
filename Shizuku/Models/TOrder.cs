using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // 為了使用 [Table] 和 [Column]

namespace Shizuku.Models
{
    [Table("tOrders")] // 告訴系統真實的資料表名稱
    public partial class TOrder
    {
        [Key]
        [Column("fId")]
        public int FId { get; set; }

        [Column("fOrder_no")]
        public string FOrderNo { get; set; } = null!;

        [Column("fMember_id")]
        public int FMemberId { get; set; }

        [Column("fTotal_amount")]
        public decimal FTotalAmount { get; set; }

        [Column("fStatus")]
        public int FStatus { get; set; }

        [Column("fReceiver_name")]
        public string FReceiverName { get; set; } = null!;

        [Column("fReceiver_phone")]
        public string FReceiverPhone { get; set; } = null!;

        [Column("fReceiver_address")]
        public string FReceiverAddress { get; set; } = null!;

        [Column("fNote")]
        public string? FNote { get; set; }

        [Column("fCreated_at")]
        public DateTime FCreatedAt { get; set; }

        [Column("fUpdated_at")]
        public DateTime FUpdatedAt { get; set; }
    }
}
