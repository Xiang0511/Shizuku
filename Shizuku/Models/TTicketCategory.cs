using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Shizuku.Models;

public partial class TTicketCategory
{

    public int FId { get; set; }
    [DisplayName("問題類型")]
    public string? FName { get; set; }
    [DisplayName("問題描述")]
    public string? FDescription { get; set; }
    [DisplayName("創建時間")]
    public DateTime? FCreatedAt { get; set; }
    public bool? FIsDeleted { get; set; }
}
