using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TLeaveRecord
{
    public int FId { get; set; }

    public int FEmployeeId { get; set; }

    public int? FLeaveType { get; set; }

    public DateTime FStartDate { get; set; }

    public DateTime FEndDate { get; set; }

    public int? FStatus { get; set; }

    public DateTime? FCreatedAt { get; set; }
}
