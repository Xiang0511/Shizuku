using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class TProduct
{
    public int FId { get; set; }

    public string? FProduct { get; set; }

    public int FCategoryId { get; set; }

    public string FName { get; set; } = null!;

    public string? FDescription { get; set; }

    public decimal FPrice { get; set; }

    public byte FStatus { get; set; }

    public DateTime? FCreatedAt { get; set; }

    public virtual ICollection<TProductVariant> TProductVariants { get; set; } = new HashSet<TProductVariant>();
    public virtual ICollection<TProductImage> TProductImages { get; set; } = new HashSet<TProductImage>();
}
