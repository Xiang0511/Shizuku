using System.ComponentModel;

namespace Shizuku.Models;

public partial class TEmployee
{
    public int FId { get; set; }
    [DisplayName("員工編號")]
    public string FNumber { get; set; } = null!;
    [DisplayName("員工姓名")]
    public string FName { get; set; } = null!;
    [DisplayName("員工密碼")]
    public string FPassword { get; set; } = null!;
    [DisplayName("員工地址")]
    public string? FAddress { get; set; }
    [DisplayName("員工信箱")]
    public string? FEmail { get; set; }
    [DisplayName("員工電話")]
    public string? FPhone { get; set; }
    [DisplayName("入職日")]
    public DateOnly? FHireDate { get; set; }
    [DisplayName("部門編號")]
    public int? FDepartmentId { get; set; }
    [DisplayName("職位編號")]
    public int? FPositionId { get; set; }
    [DisplayName("在職狀態")]
    public string FStatus { get; set; } = null!;
    [DisplayName("創建時間")]
    public DateTime? FCreatedAt { get; set; }
    [DisplayName("最後更新")]
    public DateTime? FUpdatedAt { get; set; }
}
