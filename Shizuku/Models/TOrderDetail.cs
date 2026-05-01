using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TOrderDetail
{
    public int FId { get; set; }

    public int FOrderId { get; set; }

    public int FVariantId { get; set; }

    public string FProductNameSnap { get; set; } = null!;

    public decimal FPriceSnap { get; set; }

    public int FQuantity { get; set; }

    public decimal FSubtotal { get; set; }

    public virtual TOrder FOrder { get; set; } = null!;
}
