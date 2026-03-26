using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shizuku.Models;
using Shizuku.ViewModels;
namespace Shizuku.Controllers
{
    public class AttendanceController : Controller
    {
        // 1. 顯示打卡畫面
        [HttpGet]
        public IActionResult CheckIn()
        {
            DbShizukuDemoContext db = new DbShizukuDemoContext();
            DateOnly today = DateOnly.FromDateTime(DateTime.Now);

            // 抓取今日前 10 筆打卡紀錄 (包含員工姓名)
            // 注意：這裡假設你的 TAttendanceRecord 導覽屬性是 FEmployee
            var records = db.TAttendanceRecords
                .Include(r => r.FEmployee) // 記得加上 Include 才能抓到員工姓名
                .Where(r => r.FWorkDate == today)
                .OrderByDescending(r => r.FId) // 最新的在上面
                .Take(10)
                .Select(r => new AttendanceSummaryViewModel
                {
                    EmployeeName = r.FEmployee.FName,
                    EmployeeNumber = r.FEmployee.FNumber,
                    ClockInTime = r.FClockInTime.ToString("HH:mm:ss"),
                    // 如果還沒下班（下班時間等於上班時間），顯示橫線
                    ClockOutTime = r.FClockOutTime == r.FClockInTime ? "---" : r.FClockOutTime.ToString("HH:mm:ss"),
                    Status = r.FStatus
                }).ToList();

            // 建立 ViewModel 並把清單塞進去
            var viewModel = new CheckInViewModel
            {
                TodayRecords = records
            };

            return View(viewModel); // 把 ViewModel 丟給 View
        }

        //處理打卡邏輯
        [HttpPost]
        public IActionResult CheckIn(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                TempData["ErrorMessage"] = "請輸入員工編號或信箱！";
                return RedirectToAction("CheckIn");
            }

            DbShizukuDemoContext db = new DbShizukuDemoContext();
            var employee = db.TEmployees.FirstOrDefault(p => (p.FNumber == input || p.FEmail == input) && p.FStatus != "離職");

            if (employee == null)
            {
                TempData["ErrorMessage"] = $"找不到編號 {input} 的員工，或該員工已離職。";
                return RedirectToAction("CheckIn");
            }
            //打卡邏輯 (加入時間判定規則)

            DateTime now = DateTime.Now;
            DateOnly today = DateOnly.FromDateTime(now);

            // 1. 定義公司規定的上下班時間 
            // 設定 09:00:59 為死線，超過就是 09:01 遲到
            TimeSpan workStartTime = new TimeSpan(9, 0, 59);
            TimeSpan workEndTime = new TimeSpan(18, 0, 0);   // 18:00 之後才算正常下班

            var todayRecord = db.TAttendanceRecords.FirstOrDefault(r =>
            r.FEmployeeId == employee.FId &&
            r.FWorkDate == today);

            string punchType = "";

            if (todayRecord == null)
            {

                //上班打卡 判斷是否遲到：當下時間的時分秒 > 規定的上班時間
                bool isLate = now.TimeOfDay > workStartTime;
                string currentStatus = isLate ? "遲到" : "正常";

                TAttendanceRecord newRecord = new TAttendanceRecord
                {
                    FEmployeeId = employee.FId,
                    FWorkDate = today,
                    FClockInTime = now,
                    FClockOutTime = now,
                    FCreatedAt = now,
                    FStatus = currentStatus // 寫入判斷後的狀態
                };
                db.TAttendanceRecords.Add(newRecord);
                punchType = "上班";
            }
            else
            {
                // 下班打卡 (覆寫模式)
                todayRecord.FClockOutTime = now;

                // 重新綜合判定今天的最終狀態 (必須同時考量早上有沒有遲到，跟現在有沒有早退)
                bool wasLate = todayRecord.FClockInTime.TimeOfDay > workStartTime;
                bool isEarlyLeave = now.TimeOfDay < workEndTime;

                if (wasLate && isEarlyLeave)
                {
                    todayRecord.FStatus = "遲到, 早退";
                }
                else if (wasLate)
                {
                    todayRecord.FStatus = "遲到";
                }
                else if (isEarlyLeave)
                {
                    todayRecord.FStatus = "早退";
                }
                else
                {
                    todayRecord.FStatus = "正常"; // 完美員工
                }

                punchType = "下班";
            }

            db.SaveChanges();

            //顯示成功訊息 (會動態顯示上班或下班)
            TempData["SuccessMessage"] = $"{employee.FName} 您好，{punchType}打卡成功！時間：{now.ToString("HH:mm:ss")}";

            return RedirectToAction("CheckIn");
        }

        public IActionResult History()
        {
            return View();
        }

    }
}
