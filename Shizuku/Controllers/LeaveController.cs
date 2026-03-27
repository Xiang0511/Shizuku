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
        public IActionResult Apply()
        {
            var records = db.TLeaveRecords
                .Include(r => r.FEmployee)
                .OrderByDescending(r => r.FCreatedAt)
                .Take(20) // 顯示最近 20 筆
                .Select(r => new LeaveHistoryItem
                {
                    FId = r.FId,
                    EmployeeName = r.FEmployee.FName,
                    // 轉型 Enum 顯示中文
                    LeaveTypeName = ((LeaveType)r.FLeaveType).ToString(),
                    StartDate = r.FStartDate.ToString("yyyy-MM-dd HH:mm"),
                    EndDate = r.FEndDate.ToString("yyyy-MM-dd HH:mm"),
                    StatusName = ((LeaveStatus)(r.FStatus ?? 0)).ToString(),
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

            TempData["SuccessMessage"] = $"{employee.FName} 的{((LeaveType)vm.SelectedLeaveType)}申請已送出，等待審核中。";
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