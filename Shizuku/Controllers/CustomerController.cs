using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shizuku.Models;
using Shizuku.ViewModels;



namespace Shizuku.Controllers
{
    public class CustomerController : Controller
    {
        // DbShizukuDemoContext 是你截圖中那個資料庫連線檔案
        //private readonly DbShizukuDemoContext _context;

        //public CustomerController(DbShizukuDemoContext context)
        //{
        //    _context = context;
        //}

        // R (Read): 撈出所有工單
        public IActionResult List(CKeywordViewModel vm)
        {
            ViewBag.TitleName = "所有案件一覽表";
            DbShizukuDemoContext db = new DbShizukuDemoContext();

            // 第一步：先過濾掉「已刪除」的，這是一切的基礎
            // 我們直接用 IQueryable 讓它在資料庫過濾，效率更好
            IEnumerable<TTicketsCustomer> datas = db.TTicketsCustomers
                                            .Include(t => t.FCategory)
                                            .Where(p => p.FIsDeleted != true)
                                            .ToList();

            // 第二步：如果有搜尋關鍵字，就在「沒被刪除」的資料裡繼續篩選
            if (!string.IsNullOrEmpty(vm.txtKeyword))
            {
                datas = datas.Where(p =>
                      p.FSubject.Contains(vm.txtKeyword) // 搜主旨
        || (p.FCategory != null && p.FCategory.FName.Contains(vm.txtKeyword)) // 搜分類名稱
    ).ToList();
            }

            // 最後才轉成 List 丟給 View
            return View(datas.ToList());
        }
        public IActionResult Pending(CKeywordViewModel vm)
        {
            ViewBag.TitleName = "待處理案件一覽表";
            DbShizukuDemoContext db = new DbShizukuDemoContext();

            // 1. 先把「沒被刪除」且「待處理」且「包含分類資料」的基礎撈出來
            // 加上 .Include(t => t.FCategory) 是為了讓 List.cshtml 有中文名可以用
            var baseData = db.TTicketsCustomers
                             .Include(t => t.FCategory)
                             .Where(p => p.FIsDeleted != true && p.FStatus == "待處理");

            IEnumerable<TTicketsCustomer> datas = null;

            // 2. 判斷有沒有關鍵字
            if (string.IsNullOrEmpty(vm.txtKeyword))
            {
                // 沒關鍵字：直接用剛才設定好的基礎資料
                datas = baseData.ToList();
            }
            else
            {
                // 有關鍵字：在 baseData 的基礎上繼續過濾，保留你原本所有的搜尋欄位
                datas = baseData.Where(p =>
            p.FSubject.Contains(vm.txtKeyword) ||
            (p.FCategory != null && p.FCategory.FName.Contains(vm.txtKeyword))
    ).ToList();
            }

            // 3. 借用 List 的網頁畫面來顯示
            return View("List", datas);
        }
        public IActionResult Categories(CKeywordViewModel vm)
        {
            DbShizukuDemoContext db = new DbShizukuDemoContext();

            // 1. 先抓出「沒被刪除」的分類
            var datas = db.TTicketCategories.Where(c => c.FIsDeleted != true);

            // 2. 判斷搜尋框有沒有打字
            if (!string.IsNullOrEmpty(vm.txtKeyword))
            {

                datas = datas.Where(c =>
        c.FName.Contains(vm.txtKeyword) ||       // 搜分類名字 (如：商品瑕疵)
        c.FDescription.Contains(vm.txtKeyword)   // 搜分類描述 (如：破損、污漬)
    );
            }

            // 3. 如果沒打字，它就不會跑進上面的 if，直接回傳第一步的結果
            return View(datas.ToList());
        }
        // 1. 【GET】顯示要處理的那一筆資料
        public IActionResult Edit(int? id)
        {
            if (id == null) return RedirectToAction("List");

            DbShizukuDemoContext db = new DbShizukuDemoContext();
            TTicketsCustomer x = db.TTicketsCustomers.FirstOrDefault(t => t.FId == id);

            if (x == null) return RedirectToAction("List");
            // --- 這裡開始是重點 ---
            // 1. 撈出沒被刪除的分類
            // 2. 把每一筆分類轉換成 SelectListItem (下拉選單專用的格式)
            var categoryList = db.TTicketCategories
                .Where(c => c.FIsDeleted != true)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.FId.ToString(), // 這裡存的是數字 ID (給程式看)
                    Text = c.FName            // 這裡顯示的是中文名字 (給客服看)
                }).ToList();

            // 3. 丟進 ViewBag 帶去前端
            ViewBag.CategoryOptions = categoryList;


            return View(x); // 把這筆資料帶去編輯頁面
        }

        // 2. 【POST】接收修改後的資料並存進資料庫
        [HttpPost]
        public IActionResult Edit(TTicketsCustomer t) // 這裡的 t 是從網頁傳回來的資料
        {
            DbShizukuDemoContext db = new DbShizukuDemoContext();

            // 1. 先從資料庫抓出原始的那一筆資料，一樣用你喜歡的明確寫法
            TTicketsCustomer x = db.TTicketsCustomers.FirstOrDefault(p => p.FId == t.FId);

            // 2. 如果找得到這筆資料，就開始「蓋過」它
            if (x != null)
            {
                x.FCategoryId = t.FCategoryId; //蓋過問題分類
                x.FSubject = t.FSubject;       // 蓋過主旨
                x.FStatus = t.FStatus;         // 蓋過狀態
                x.FPriority = t.FPriority;     // 蓋過優先等級
                x.FAssignedAgentId = t.FAssignedAgentId; // 蓋過經辦人

                // 額外加這個：記錄最後更新時間
                x.FUpdatedAt = DateTime.Now;

                // 3. 核心動作：告訴資料庫「我要存檔了」
                db.SaveChanges();
            }

            // 4. 存完之後，跳轉回列表頁面
            return RedirectToAction("List");
        }
        // 1. 【GET】顯示要編輯的分類資料
        public IActionResult CategoryEdit(int? id)
        {
            if (id == null) return RedirectToAction("Categories");

            DbShizukuDemoContext db = new DbShizukuDemoContext();

            // 明確寫出型別，不使用 var
            TTicketCategory x = db.TTicketCategories.FirstOrDefault(c => c.FId == id);

            if (x == null) return RedirectToAction("Categories");

            return View(x);
        }

        // 2. 【POST】儲存分類修改
        [HttpPost]
        public IActionResult CategoryEdit(TTicketCategory t)
        {
            DbShizukuDemoContext db = new DbShizukuDemoContext();

            // 明確寫出型別，不使用 var
            TTicketCategory x = db.TTicketCategories.FirstOrDefault(c => c.FId == t.FId);

            if (x != null)
            {
                x.FName = t.FName;               // 修改分類名稱
                x.FDescription = t.FDescription; // 修改描述
                db.SaveChanges();
            }
            return RedirectToAction("Categories");
        }
        public IActionResult Delete(int? id)
        {
            if (id == null) return RedirectToAction("List");

            DbShizukuDemoContext db = new DbShizukuDemoContext();
            TTicketsCustomer x = db.TTicketsCustomers.FirstOrDefault(t => t.FId == id);

            if (x != null)
            {
                x.FIsDeleted = true; // 標記為已刪除
                db.SaveChanges();
            }
            return RedirectToAction("List");
        }
        public IActionResult CategoryDelete(int? id)
        {
            if (id == null) return RedirectToAction("Categories");

            DbShizukuDemoContext db = new DbShizukuDemoContext();
            TTicketCategory x = db.TTicketCategories.FirstOrDefault(c => c.FId == id);

            if (x != null)
            {
                x.FIsDeleted = true; // 標記為已刪除
                db.SaveChanges();
            }
            return RedirectToAction("Categories");
        }
    }
}