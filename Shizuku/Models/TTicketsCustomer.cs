using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TTicketsCustomer
{
    public int FId { get; set; }

    public int? FMemberId { get; set; }

    public int? FOrderId { get; set; }

    public int? FCategoryId { get; set; }

    public string? FSubject { get; set; }

    public string? FStatus { get; set; }

    public string? FPriority { get; set; }

    public int? FAssignedAgentId { get; set; }

    public DateTime? FCreatedAt { get; set; }

    public DateTime? FUpdatedAt { get; set; }

    public DateTime? FClosedAt { get; set; }
}
