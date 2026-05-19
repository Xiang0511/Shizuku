using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Shizuku.Models;

public partial class TTicketsCustomer
{
    [DisplayName("案件編號")]
    public int FId { get; set; }

    [DisplayName("會員編號")]
    public int FMemberId { get; set; }

    [DisplayName("訂單編號")]
    public int? FOrderId { get; set; }

    [DisplayName("問題類型")]
    public int? FCategoryId { get; set; }

    public virtual TTicketCategory FCategory { get; set; }

    // ==========================================
    //  我們新增的 3 個訪客專用欄位放這邊
    // ==========================================
    [DisplayName("訪客姓名")]
    public string? FGuestName { get; set; }

    [DisplayName("訪客信箱")]
    public string? FGuestEmail { get; set; }

    [DisplayName("主旨")]
    public string? FSubject { get; set; }

    [DisplayName("詳細描述")]
    public string? FDescription { get; set; }
    // ==========================================

    [DisplayName("處理狀態")]
    public string? FStatus { get; set; }

    [DisplayName("優先等級")]
    public string? FPriority { get; set; }

    [DisplayName("經辦客服")]
    public int? FAssignedAgentId { get; set; }

    [DisplayName("建立時間")]
    public DateTime? FCreatedAt { get; set; }

    [DisplayName("最後更新")]
    public DateTime? FUpdatedAt { get; set; }

    [DisplayName("結束時間")]
    public DateTime? FClosedAt { get; set; }

    public bool? FIsDeleted { get; set; }
}