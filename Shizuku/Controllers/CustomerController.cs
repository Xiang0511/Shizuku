using Microsoft.AspNetCore.Mvc;
using Shizuku.Models;
using Shizuku.ViewModels;
using Shizuku.Services;
using Shizuku.Wraps;

namespace Shizuku.Controllers
{
    public class CustomerController : Controller
    {
        private readonly CustomerService _customerService;

        public CustomerController()
        {
            // 依照規範暫時在此初始化
            _customerService = new CustomerService(new DbShizukuDemoContext());
        }

        // 1. 所有案件列表
        public IActionResult List(CKeywordViewModel vm)
        {
            ViewBag.TitleName = "所有案件一覽表";
            // Service 現在回傳 List<CTicketCustomerWrap>
            var datas = _customerService.GetTickets(vm.txtKeyword);
            return View(datas);
        }

        // 2. 待處理案件列表
        public IActionResult Pending(CKeywordViewModel vm)
        {
            ViewBag.TitleName = "待處理案件一覽表";
            // Service 現在回傳 List<CTicketCustomerWrap>
            var datas = _customerService.GetTickets(vm.txtKeyword, "待處理");
            return View("List", datas);
        }

        // 3. 分類管理列表
        public IActionResult Categories(CKeywordViewModel vm)
        {
            // Service 現在回傳 List<CTicketCategoryWrap>
            var datas = _customerService.GetCategories(vm.txtKeyword);
            return View(datas);
        }

        // 4. 編輯案件 (GET)
        public IActionResult Edit(int? id)
        {
            if (id == null) return RedirectToAction("List");

            // Service 現在回傳 CTicketCustomerWrap
            var ticketWrap = _customerService.GetTicketById((int)id);
            if (ticketWrap == null) return RedirectToAction("List");

            ViewBag.CategoryOptions = _customerService.GetCategorySelectList();
            return View(ticketWrap);
        }

        // 5. 編輯案件 (POST)
        [HttpPost]
        public IActionResult Edit(CTicketCustomerWrap wrap)
        {
            // 這裡接收的參數必須改成 Wrap，才能對應 View 傳過來的 f 欄位
            _customerService.UpdateTicket(wrap);
            return RedirectToAction("List");
        }

        // 6. 編輯分類 (GET)
        public IActionResult CategoryEdit(int? id)
        {
            if (id == null) return RedirectToAction("Categories");

            // Service 現在回傳 CTicketCategoryWrap
            var categoryWrap = _customerService.GetCategoryById((int)id);
            if (categoryWrap == null) return RedirectToAction("Categories");

            return View(categoryWrap);
        }

        // 7. 編輯分類 (POST)
        [HttpPost]
        public IActionResult CategoryEdit(CTicketCategoryWrap wrap)
        {
            // 這裡接收的參數必須改成 Wrap
            _customerService.UpdateCategory(wrap);
            return RedirectToAction("Categories");
        }

        // 8. 刪除案件
        public IActionResult Delete(int? id)
        {
            if (id != null)
            {
                _customerService.DeleteTicket((int)id);
            }
            return RedirectToAction("List");
        }

        // 9. 刪除分類
        public IActionResult CategoryDelete(int? id)
        {
            if (id != null)
            {
                _customerService.DeleteCategory((int)id);
            }
            return RedirectToAction("Categories");
        }
        //// 10 . 聊天室 
        //public IActionResult Chat()
        //{
        //    return View();
        //}
    }
}