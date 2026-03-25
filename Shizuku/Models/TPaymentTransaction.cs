using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TPaymentTransaction
{
    public int FId { get; set; }

    public string FTransactionNo { get; set; } = null!;

    public int FOrderId { get; set; }

    public int FMemberId { get; set; }

    public int FMethodId { get; set; }

    public decimal FAmount { get; set; }

    public string? FGatewayTradeNo { get; set; }

    public int FStatus { get; set; }

    public DateTime? FPaidAt { get; set; }

    public DateTime FCreatedAt { get; set; }
}
