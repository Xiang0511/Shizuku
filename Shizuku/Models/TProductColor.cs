using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TProductColor
{
    public int FId { get; set; }

    public string FName { get; set; } = null!;

    public string? FColorCode { get; set; }
}
