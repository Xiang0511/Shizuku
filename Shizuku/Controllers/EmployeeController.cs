using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shizuku.Models;
using Shizuku.Services; 
using Shizuku.ViewModels;

namespace Shizuku.Controllers
{
    public class EmployeeController : Controller // 將原本小寫 e 的 employeeController 改為大寫 E
    {
        private readonly EmployeeService _employeeService;

        // 透過 DI 注入 EmployeeService
        public EmployeeController(EmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        // 員工清單 & 搜尋欄
        public IActionResult List(CKeywordViewModel vm, string statusFilter = "Active")
        {
            ViewBag.StatusFilter = statusFilter;

            // 將複雜的查詢邏輯交給 Service
            var datas = _employeeService.GetEmployees(vm.txtKeyword, statusFilter);

            return View(datas);
        }

        // 新建員工 (顯示表單)
        public IActionResult Create()
        {
            ViewBag.DepartmentList = new SelectList(_employeeService.GetAllDepartments(), "FId", "FDepartmentName");
            ViewBag.PositionList = new SelectList(_employeeService.GetAllPositions(), "FId", "FPositionName");
            
            TEmployee defaultEmployee = new TEmployee()
            {
                FStatus = "在職" 
            };
            return View(defaultEmployee);
        }

        // 新建員工 (處理送出資料)
        [HttpPost]
        public IActionResult Create(TEmployee p)
        {
            // 移除不需要使用者填寫的驗證
            ModelState.Remove("FStatus");
            ModelState.Remove(nameof(p.FNumber));
            ModelState.Remove(nameof(p.FStatus));
            ModelState.Remove(nameof(p.FPassword));
            ModelState.Remove(nameof(p.FCreatedAt));

            if (ModelState.IsValid)
            {
                // 將資料庫寫入與編號產生邏輯交給 Service
                _employeeService.CreateEmployee(p);
                return RedirectToAction("List");
            }

            // 驗證失敗時，重新準備下拉選單資料
            ViewBag.DepartmentList = new SelectList(_employeeService.GetAllDepartments(), "FId", "FDepartmentName", p.FDepartmentId);
            ViewBag.PositionList = new SelectList(_employeeService.GetAllPositions(), "FId", "FPositionName", p.FPositionId);

            return View(p);
        }

        // 修改員工 (顯示表單)
        public IActionResult Edit(int? id)
        {
            if (id == null) return RedirectToAction("List");

            TEmployee x = _employeeService.GetEmployeeById(id.Value);

            if (x == null) return RedirectToAction("List");

            // 準備部門與職位的下拉選單資料交給 Service 處理
            ViewBag.DepartmentList = new SelectList(_employeeService.GetAllDepartments(), "FId", "FDepartmentName");
            ViewBag.PositionList = new SelectList(_employeeService.GetAllPositions(), "FId", "FPositionName");

            return View(x);
        }

        // 修改員工 (處理送出資料)
        [HttpPost]
        public IActionResult Edit(TEmployee e)
        {
            if (ModelState.IsValid)
            {
                // 更新邏輯交給 Service
                _employeeService.UpdateEmployee(e);
                return RedirectToAction("List");
            }

            // 若驗證失敗，重新準備下拉選單
            ViewBag.DepartmentList = new SelectList(_employeeService.GetAllDepartments(), "FId", "FDepartmentName", e.FDepartmentId);
            ViewBag.PositionList = new SelectList(_employeeService.GetAllPositions(), "FId", "FPositionName", e.FPositionId);

            return View(e);
        }

        // 軟刪除,隱藏已離職員工
        public IActionResult Delete(int? id)
        {
            if (id == null) return RedirectToAction("List");

            // 軟刪除邏輯交給 Service
            _employeeService.SoftDeleteEmployee(id.Value);
            
            return RedirectToAction("List");
        }
    }
}
