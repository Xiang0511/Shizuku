namespace Shizuku.ViewModels
{
    public class CheckInViewModel
    {
        // 這裡放置今日的打卡紀錄清單
        public List<AttendanceSummaryViewModel> TodayRecords { get; set; } = new List<AttendanceSummaryViewModel>();
    }

    public class AttendanceSummaryViewModel
    {
        public string EmployeeName { get; set; }
        public string EmployeeNumber { get; set; }
        public string ClockInTime { get; set; }
        public string ClockOutTime { get; set; }
        public string Status { get; set; }
    }
}