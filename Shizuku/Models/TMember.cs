using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TMember
{
    public int FId { get; set; }

    public string? FMemberId { get; set; }

    public string? FAccount { get; set; }

    public string? FPassword { get; set; }

    public string? FName { get; set; }

    public string? FEmail { get; set; }

    public string? FPhone { get; set; }

    public DateOnly? FBirthday { get; set; }

    public int? FGender { get; set; }

    public int? FLevel { get; set; }

    public DateTime? FCreatedTime { get; set; }

    public DateTime? FUpdatedTime { get; set; }

    public bool? FIsActive { get; set; }

    public string? FReceiverName { get; set; }

    public string? FReceiverPhone { get; set; }

    public string? FReceiverAddress { get; set; }

    public DateTime? FLoginTime { get; set; }

    public string? FIpAddress { get; set; }

    public string? FWishlist { get; set; }

    public string? FImage { get; set; }
}
