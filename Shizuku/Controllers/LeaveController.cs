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
    }
}