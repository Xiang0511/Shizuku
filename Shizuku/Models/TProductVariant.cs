using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shizuku.Models;

public partial class TProductVariant
{
    public int FId { get; set; }

    public int FProductId { get; set; }

    public int FColorId { get; set; }

    public int FSizeId { get; set; }

    public string FSkuCode { get; set; } = null!;

    public int FStock { get; set; }

    public decimal? FPrice { get; set; }
}
