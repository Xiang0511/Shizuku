using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TPaymentLog
{
    public int FId { get; set; }

    public int FPaymentTransactionsId { get; set; }

    public string FActionType { get; set; } = null!;

    public string? FRequestData { get; set; }

    public string? FResponseData { get; set; }

    public DateTime FCreatedAt { get; set; }
}
