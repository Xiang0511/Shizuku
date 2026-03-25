using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shizuku.Models;
using Shizuku.ViewModels;

namespace testCshap.Controllers
{
    public class employeeController : Controller
    {
        //員工清單&搜尋欄
        // [修改範圍：List 方法]
        // 參數新增 string statusFilter，並給予預設值 "Active"
        public IActionResult List(CKeywordViewModel vm, string statusFilter = "Active")
        {
            DbShizukuDemoContext db = new DbShizukuDemoContext();

            // 將目前的過濾狀態存入 ViewBag，讓前端的下拉選單能維持選取狀態
            ViewBag.StatusFilter = statusFilter;

            // ==========================================
            // 步驟 1：建立基礎查詢 (Base Query)
            // ==========================================
            var query = db.TEmployees.AsQueryable();

            // ==========================================
            // 步驟 2：根據下拉選單進行狀態過濾 (核心邏輯升級)
            // ==========================================
            if (statusFilter == "Active")
            {
                // 僅顯示在職 (過濾掉離職)
                query = query.Where(p => p.FStatus != "離職");
            }
            else if (statusFilter == "Resigned")
            {
                // 僅顯示離職
                query = query.Where(p => p.FStatus == "離職");
            }
            // 如果是 "All"，則不加任何狀態過濾條件，直接往下走

            // ==========================================
            // 步驟 3：疊加關鍵字搜尋條件
            // ==========================================
            if (!string.IsNullOrEmpty(vm.txtKeyword))
            {
                query = query.Where(p =>
                    p.FNumber.Contains(vm.txtKeyword) ||
                    p.FName.Contains(vm.txtKeyword) ||
                    p.FAddress.Contains(vm.txtKeyword) ||
                    p.FPhone.Contains(vm.txtKeyword) ||
                    p.FEmail.Contains(vm.txtKeyword) ||
                    p.FStatus.Contains(vm.txtKeyword) ||
                    p.FDepartmentId.ToString().Contains(vm.txtKeyword) ||
                    p.FHireDate.ToString().Contains(vm.txtKeyword)
                );
            }

            // ==========================================
            // 步驟 4：執行查詢並回傳
            // ==========================================
            var datas = query.ToList();

            return View(datas);
        }

        //新建員工
        public IActionResult Create()
        {
            DbShizukuDemoContext db = new DbShizukuDemoContext();
            ViewBag.DepartmentList = new SelectList(db.TDepartments.ToList(), "FId", "FDepartmentName");
            ViewBag.PositionList = new SelectList(db.TPositions.ToList(), "FId", "FPositionName");
            TEmployee defaultEmployee = new TEmployee()
            {
                FStatus = "在職" // 設定預設狀態為「在職」
            };
            return View(defaultEmployee);
        }
        [HttpPost]
        public IActionResult Create(TEmployee p)
        {
            DbShizukuDemoContext db = new DbShizukuDemoContext();


            p.FStatus = "在職";

            ModelState.Remove("FStatus");
            if (ModelState.IsValid)
            {
                db.TEmployees.Add(p);
                db.SaveChanges();
                return RedirectToAction("List");
            }
            ViewBag.DepartmentList = new SelectList(db.TDepartments.ToList(), "FId", "FDepartmentName", p.FDepartmentId);
            ViewBag.PositionList = new SelectList(db.TPositions.ToList(), "FId", "FPositionName", p.FPositionId);

            return View(p);
        }

        //修改員工
        public IActionResult Edit(int? id)
        {
            if (id == null) return RedirectToAction("List");

            DbShizukuDemoContext db = new DbShizukuDemoContext();
            TEmployee x = db.TEmployees.FirstOrDefault(p => p.FId == id);

            if (x == null)
                return RedirectToAction("List");

            // 準備部門與職位的下拉選單資料
            ViewBag.DepartmentList = new SelectList(db.TDepartments.ToList(), "FId", "FDepartmentName");
            ViewBag.PositionList = new SelectList(db.TPositions.ToList(), "FId", "FPositionName");

            return View(x);
        }
        [HttpPost]
        public IActionResult Edit(TEmployee e)
        {
            DbShizukuDemoContext db = new DbShizukuDemoContext();

            // 確保表單驗證成功才進行資料庫寫入
            if (ModelState.IsValid)
            {
                TEmployee dbEmployee = db.TEmployees.FirstOrDefault(p => p.FId == e.FId);
                if (dbEmployee != null)
                {
                    // 更新基本資料
                    dbEmployee.FName = e.FName;
                    dbEmployee.FPassword = e.FPassword;
                    dbEmployee.FPhone = e.FPhone;
                    dbEmployee.FEmail = e.FEmail;
                    dbEmployee.FAddress = e.FAddress;


                    // [新增] 更新部門、職位與狀態
                    dbEmployee.FDepartmentId = e.FDepartmentId;
                    dbEmployee.FPositionId = e.FPositionId;
                    dbEmployee.FStatus = e.FStatus; // 修改頁面必須允許變更狀態 (例如：離職)
                    dbEmployee.FUpdatedAt = DateTime.Now;

                    db.SaveChanges();
                }
                return RedirectToAction("List");
            }
            // 若驗證失敗，需要重新準備下拉選單的資料，否則返回 View 時會報錯
            ViewBag.DepartmentList = new SelectList(db.TDepartments.ToList(), "FId", "FDepartmentName", e.FDepartmentId);
            ViewBag.PositionList = new SelectList(db.TPositions.ToList(), "FId", "FPositionName", e.FPositionId);

            return View(e);
        }
        //軟刪除,隱藏已離職員工
        public IActionResult Delete(int? id)
        {
            //  基礎防呆，避免 id 為 null 時發生錯誤
            if (id == null) return RedirectToAction("List");

            DbShizukuDemoContext db = new DbShizukuDemoContext();
            TEmployee x = db.TEmployees.FirstOrDefault(p => p.FId == id);

            if (x != null)
            {
                x.FStatus = "離職";
                x.FUpdatedAt = DateTime.Now;
                db.SaveChanges();
            }
            return RedirectToAction("List");
        }
    }
}
