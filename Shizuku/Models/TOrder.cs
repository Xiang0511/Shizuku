using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TOrder
{
    public int FId { get; set; }

    public string FOrderNo { get; set; } = null!;

    public int FMemberId { get; set; }

    public decimal FTotalAmount { get; set; }

    public int FStatus { get; set; }

    public string FReceiverName { get; set; } = null!;

    public string FReceiverPhone { get; set; } = null!;

    public string FReceiverAddress { get; set; } = null!;

    public string? FNote { get; set; }

    public DateTime FCreatedAt { get; set; }

    public DateTime FUpdatedAt { get; set; }

    public virtual ICollection<TOrderDetail> TOrderDetails { get; set; } = new List<TOrderDetail>();
}
