using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TTicketMessage
{
    public int FId { get; set; }

    public int? FTicketId { get; set; }

    public int? FSenderId { get; set; }

    public string? FMessage { get; set; }

    public bool? FIsRead { get; set; }

    public DateTime? FCreatedAt { get; set; }
}
