using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TEmployee
{
    public int FId { get; set; }

    public string FNumber { get; set; } = null!;

    public string FName { get; set; } = null!;

    public string FPassword { get; set; } = null!;

    public string? FEmail { get; set; }

    public string? FPhone { get; set; }

    public DateOnly? FHireDate { get; set; }

    public int? FDepartmentId { get; set; }

    public int? FPositionId { get; set; }

    public string FStatus { get; set; } = null!;

    public DateTime FCreatedAt { get; set; }

    public DateTime? FUpdatedAt { get; set; }

    public string? FAddress { get; set; }
}
