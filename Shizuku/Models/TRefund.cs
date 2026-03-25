using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TRefund
{
    public int FId { get; set; }

    public string FRefundNo { get; set; } = null!;

    public int FTransactionId { get; set; }

    public int FOrderId { get; set; }

    public int FMemberId { get; set; }

    public decimal FRefundAmount { get; set; }

    public string? FReason { get; set; }

    public int FStatus { get; set; }

    public DateTime? FProcessedAt { get; set; }
}
