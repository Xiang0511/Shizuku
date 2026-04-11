namespace Shizuku.ViewModels
{
    public class LeaveViewModel
    {
        // 用於顯示清單的紀錄
        public List<LeaveHistoryItem> LeaveRecords { get; set; } = new();

        // 用於申請表單的欄位
        public string EmployeeNumber { get; set; }
        public int SelectedLeaveType { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddHours(1);
    }

    public class LeaveHistoryItem
    {
        public int FId { get; set; }
        public string EmployeeName { get; set; }
        public string LeaveTypeName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string StatusName { get; set; }
        public string CreatedAt { get; set; }
        public string TotalHours { get; set; }
    }
}