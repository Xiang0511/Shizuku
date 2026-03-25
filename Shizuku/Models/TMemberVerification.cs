using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TMemberVerification
{
    public int FId { get; set; }

    public int? FMemberId { get; set; }

    public string? FCode { get; set; }

    public int? FType { get; set; }

    public DateTime? FExpireTime { get; set; }

    public int? FAttemptCount { get; set; }

    public bool? FIsUsed { get; set; }

    public DateTime? FCreatedTime { get; set; }
}
