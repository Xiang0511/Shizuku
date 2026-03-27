using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class SystemLog
{
    public int Id { get; set; }

    public string? Message { get; set; }

    public string? MessageTemplate { get; set; }

    public string? Level { get; set; }

    public DateTime Timestamp { get; internal set; }
    public string? Exception { get; set; }

    public string? Properties { get; set; }
}
