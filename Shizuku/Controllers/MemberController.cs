using Microsoft.AspNetCore.Mvc;
using Shizuku.Models;
using Shizuku.ViewModels;
using Shizuku.Wrap;

namespace Shizuku.Controllers
{
    public class MemberController : Controller
    {
        private readonly DbShizukuDemoContext _DBcontext;

        public MemberController(DbShizukuDemoContext context)
        {
            _DBcontext = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult List(CKeywordViewModel vm)
        {

            //DbShizukuDemoContext db = new DbShizukuDemoContext();

            IQueryable<TMember> query = _DBcontext.TMembers.Where(p => p.FIsActive == true);

            if (!string.IsNullOrEmpty(vm.txtKeyword))
            {
                string k = vm.txtKeyword;
                // 搜尋：姓名、Email、電話 或 會員編號 (模糊比對)
                query = query.Where(p => p.FName.Contains(k)
                                      || p.FEmail.Contains(k)
                                      || p.FPhone.Contains(k)
                                      || p.FMemberId.Contains(k));
            }

            List<CMemberWrap> datas = new List<CMemberWrap>();
            foreach (var p in query)
            {
                datas.Add(new CMemberWrap { member = p });
            }

            return View(datas);
        }
        public IActionResult Block_List(CKeywordViewModel vm)
        {
            //DbShizukuDemoContext db = new DbShizukuDemoContext();
            IQueryable<TMember> query = _DBcontext.TMembers.Where(p => p.FIsActive == false);
            if (!string.IsNullOrEmpty(vm.txtKeyword))
            {
                string k = vm.txtKeyword;
                query = query.Where(p => p.FName.Contains(k)
                                      || p.FEmail.Contains(k)
                                      || p.FPhone.Contains(k)
                                      || p.FMemberId.Contains(k));
            }
            List<CMemberWrap> datas = new List<CMemberWrap>();
            foreach (var p in query)
            {
                datas.Add(new CMemberWrap { member = p });
            }
            return View(datas);
        }
        public IActionResult Delete(int? id)
        {
            //DbShizukuDemoContext db = new DbShizukuDemoContext();
            TMember x = _DBcontext.TMembers.FirstOrDefault(p => p.FId == id);
            if (x != null)
            {
                x.FIsActive = false;
                _DBcontext.SaveChanges();
            }
            return RedirectToAction("List");
        }

        public IActionResult Restore(int? id)
        {
            //DbShizukuDemoContext db = new DbShizukuDemoContext();
            TMember x = _DBcontext.TMembers.FirstOrDefault(p => p.FId == id);
            if (x != null)
            {
                x.FIsActive = true;
                _DBcontext.SaveChanges();
            }
            return RedirectToAction("Block_List");
        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(CMemberWrap p)
        {
            // 檢查驗證是否通過
            if (!ModelState.IsValid)
            {
                return View(p); // 驗證失敗，返回原頁面，會帶入剛才填的資料與錯誤訊息
            }

            //DbShizukuDemoContext db = new DbShizukuDemoContext();
            _DBcontext.TMembers.Add(p.member);
            _DBcontext.SaveChanges();

            int padLength = 3; //長度編號為:M0000 未來長度只改這行 2代表M00 3代表M000
            p.FMemberId = "M" + p.FId.ToString("D" + padLength);    //0001 0010 0100...解決格式化問題
            p.FAccount = p.FEmail;

            _DBcontext.SaveChanges();
            return RedirectToAction("List");
        }

        public IActionResult Edit(int? id)
        {
            if (id == null)
                return RedirectToAction("List");

            //DbShizukuDemoContext db = new DbShizukuDemoContext();
            TMember x = _DBcontext.TMembers.FirstOrDefault(p => p.FId == id);
            if (x == null)
                return RedirectToAction("List");

            CMemberWrap wrapper = new CMemberWrap();
            wrapper.member = x;
            return View(wrapper);
        }
        [HttpPost]
        public IActionResult Edit(CMemberWrap uiCustomer)
        {
            if (!ModelState.IsValid)
            {
                return View(uiCustomer); // 驗證失敗，返回編輯頁
            }

            //DbShizukuDemoContext db = new DbShizukuDemoContext();
            TMember dbCustomer = _DBcontext.TMembers.FirstOrDefault(p => p.FId == uiCustomer.FId);
            if (dbCustomer != null)
            {
                dbCustomer.FName = uiCustomer.FName;
                dbCustomer.FEmail = uiCustomer.FEmail;
                dbCustomer.FPassword = uiCustomer.FPassword;
                dbCustomer.FPhone = uiCustomer.FPhone;
                _DBcontext.SaveChanges();
            }
            return RedirectToAction("List");
        }

        [HttpPost]
        public IActionResult UpdateLevelAjax(int id, int newLevel)
        {
            //DbShizukuDemoContext db = new DbShizukuDemoContext();
            var member = _DBcontext.TMembers.FirstOrDefault(p => p.FId == id);

            if (member != null)
            {
                member.FLevel = newLevel;
                _DBcontext.SaveChanges();
                return Json(new { success = true, message = "等級已更新" });
            }

            return Json(new { success = false, message = "找不到該會員" });
        }
    }
}
