using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shizuku.Models
{
    [Table("tOrderDetails")]
    public partial class TOrderDetail
    {
        [Key]
        [Column("fId")]
        public int FId { get; set; }

        [Column("fOrder_id")]
        public int FOrderId { get; set; }

        [Column("fVariant_id")]
        public int FVariantId { get; set; }

        [Column("fProduct_name_snap")]
        public string FProductNameSnap { get; set; } = null!;

        [Column("fPrice_snap")]
        public decimal FPriceSnap { get; set; }

        [Column("fQuantity")]
        public int FQuantity { get; set; }

        [Column("fSubtotal")]
        public decimal FSubtotal { get; set; }
    }
}
