using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TProductSize
{
    public int FId { get; set; }

    public string FName { get; set; } = null!;

    public int FSortOrder { get; set; }
}
