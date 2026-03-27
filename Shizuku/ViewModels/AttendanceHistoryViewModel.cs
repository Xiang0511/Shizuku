namespace Shizuku.ViewModels
{
    public class AttendanceHistoryViewModel
    {
        // 1. 搜尋條件 (讓使用者輸入)
        public string? SearchEmployee { get; set; } // 編號或姓名
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }

        // 2. 搜尋結果 
        public List<AttendanceHistoryRowViewModel> Results { get; set; } = new();
    }

    public class AttendanceHistoryRowViewModel
    {
        public string WorkDate { get; set; } // 歷史頁面必須顯示「哪一天」
        public string EmployeeNumber { get; set; }
        public string EmployeeName { get; set; }
        public string ClockInTime { get; set; }
        public string ClockOutTime { get; set; }
        public string Status { get; set; }
    }
}