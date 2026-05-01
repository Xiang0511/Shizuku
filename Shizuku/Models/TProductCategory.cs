using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TProductCategory
{
    public int FId { get; set; }

    public string FName { get; set; } = null!;

    public string? FParentId { get; set; }

    public DateTime? FCreatedAt { get; set; }

    public string? FCodePrefix { get; set; }
}
