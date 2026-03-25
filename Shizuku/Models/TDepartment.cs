using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TDepartment
{
    public int FId { get; set; }

    public string? FDepartmentName { get; set; }

    public int? FManagerId { get; set; }
}
