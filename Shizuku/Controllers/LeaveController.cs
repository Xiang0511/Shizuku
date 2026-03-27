using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shizuku.Enums;
using Shizuku.Models;
using Shizuku.ViewModels;

namespace Shizuku.Controllers
{
    public class LeaveController : Controller
    {
        private readonly DbShizukuDemoContext db = new DbShizukuDemoContext();

        // 1. 顯示請假頁面
        [HttpGet]
        public IActionResult Apply(string statusFilter = "Pending") // 預設只看待審核
        {
            ViewBag.StatusFilter = statusFilter;

            var query = db.TLeaveRecords.Include(r => r.FEmployee).AsQueryable();

            // 邏輯篩選
            if (statusFilter == "Pending")
            {
                query = query.Where(r => r.FStatus == (int)LeaveStatus.待審核);
            }
            else if (statusFilter == "History")
            {
                // 顯示已核准(1) 與 駁回(2)
                query = query.Where(r => r.FStatus == (int)LeaveStatus.已核准 || r.FStatus == (int)LeaveStatus.駁回);
            }

            var records = query
                .OrderByDescending(r => r.FCreatedAt)
                .AsEnumerable()
                .Select(r => new LeaveHistoryItem
                {
                    FId = r.FId,
                    EmployeeName = r.FEmployee.FName,
                    LeaveTypeName = ((LeaveType)r.FLeaveType).ToString(),
                    StartDate = r.FStartDate.ToString("yyyy-MM-dd HH:mm"),
                    EndDate = r.FEndDate.ToString("yyyy-MM-dd HH:mm"),
                    StatusName = ((LeaveStatus)(r.FStatus ?? 0)).ToString(),
                    TotalHours = (r.FEndDate - r.FStartDate).TotalHours.ToString("N1"),
                    CreatedAt = r.FCreatedAt.HasValue ? r.FCreatedAt.Value.ToString("yyyy-MM-dd") : ""
                }).ToList();

            var viewModel = new LeaveViewModel { LeaveRecords = records };
            return View(viewModel);
        }

        // 2. 處理請假申請
        [HttpPost]
        public IActionResult Apply(LeaveViewModel vm)
        {
            if (string.IsNullOrEmpty(vm.EmployeeNumber))
            {
                TempData["ErrorMessage"] = "請輸入員工編號！";
                return RedirectToAction("Apply");
            }

            // 找員工
            var employee = db.TEmployees.FirstOrDefault(e => e.FNumber == vm.EmployeeNumber && e.FStatus != "離職");
            if (employee == null)
            {
                TempData["ErrorMessage"] = "找不到該員工編號。";
                return RedirectToAction("Apply");
            }

            // 檢查時間邏輯
            if (vm.EndDate <= vm.StartDate)
            {
                TempData["ErrorMessage"] = "結束時間必須晚於開始時間。";
                return RedirectToAction("Apply");
            }

            // --- 開始計算請假時數 ---
            double totalHours = 0;
            DateTime currentStart = vm.StartDate;
            DateTime currentEnd = vm.EndDate;

            // 1. 計算總差距
            TimeSpan duration = currentEnd - currentStart;
            totalHours = duration.TotalHours;

            // 2. 扣除午休時間 (簡單邏輯：只要請假跨越了 12:00 ~ 13:00 就扣 1 小時)
            // 這裡我們只處理「同一天」的情況，這對大多數假單已足夠
            if (currentStart.Date == currentEnd.Date)
            {
                // 如果開始時間在 12:00 以前，且結束時間在 13:00 以後
                if (currentStart.Hour < 12 && currentEnd.Hour >= 13)
                {
                    totalHours -= 1;
                }
            }
            else
            {
                // 如果跨天，邏輯會變得很複雜（需扣除每天的午休），暫時以總時數計算
                // 建議實務上請假盡量拆開按天請
            }

            // 建立紀錄
            TLeaveRecord newRecord = new TLeaveRecord
            {
                FEmployeeId = employee.FId,
                FLeaveType = vm.SelectedLeaveType,
                FStartDate = vm.StartDate,
                FEndDate = vm.EndDate,
                FStatus = (int)LeaveStatus.待審核,
                FCreatedAt = DateTime.Now
            };

            db.TLeaveRecords.Add(newRecord);
            db.SaveChanges();


            TempData["SuccessMessage"] = $"{employee.FName} 的{((LeaveType)vm.SelectedLeaveType)}申請已送出，等待審核中。共計 {totalHours:N1} 小時。";
            return RedirectToAction("Apply");
        }


        //假單審核
        [HttpGet]
        public IActionResult Review()
        {
            // 抓出所有「待審核」的假單 (Status = 0)
            var pendingRecords = db.TLeaveRecords
                .Include(r => r.FEmployee)
                .Where(r => r.FStatus == (int)LeaveStatus.待審核) // 只看待審核
                .OrderBy(r => r.FStartDate) // 越早要請的排越前面
                .Select(r => new LeaveHistoryItem
                {
                    FId = r.FId,
                    EmployeeName = r.FEmployee.FName,
                    LeaveTypeName = ((LeaveType)r.FLeaveType).ToString(),
                    StartDate = r.FStartDate.ToString("yyyy-MM-dd HH:mm"),
                    EndDate = r.FEndDate.ToString("yyyy-MM-dd HH:mm"),
                    StatusName = ((LeaveStatus)r.FStatus).ToString(),
                    CreatedAt = r.FCreatedAt.HasValue ? r.FCreatedAt.Value.ToString("yyyy-MM-dd") : ""
                }).ToList();

            return View(pendingRecords);
        }

        // 4. 執行審核動作 (POST)
        [HttpPost]
        public IActionResult UpdateStatus(int id, int status)
        {
            // 找那一筆假單
            var record = db.TLeaveRecords.Find(id);

            if (record != null)
            {
                // 更新狀態 (1: 已核准, 2: 駁回)
                record.FStatus = status;
                db.SaveChanges();

                string statusText = (status == (int)LeaveStatus.已核准) ? "已核准" : "已駁回";
                TempData["SuccessMessage"] = $"假單編號 {id} {statusText} 成功！";
            }
            else
            {
                TempData["ErrorMessage"] = "找不到該筆假單。";
            }

            return RedirectToAction("Review");
        }

    }
}