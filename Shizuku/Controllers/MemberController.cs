using Microsoft.AspNetCore.Mvc;
using Shizuku.Models;
using Shizuku.ViewModels;

namespace Shizuku.Controllers
{
    public class MemberController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult List(CKeywordViewModel vm)
        {

            DbShizukuDemoContext db = new DbShizukuDemoContext();

            var query = db.TMembers;

            List<CMemberWrap> datas = new List<CMemberWrap>();
            foreach (var p in query)
            {
                datas.Add(new CMemberWrap { member = p });
            }

            return View(datas);
        }
        public IActionResult Block_List(CKeywordViewModel vm)
        {
            DbShizukuDemoContext db = new DbShizukuDemoContext();
            IEnumerable<TMember> datas = null;
            datas = from p in db.TMembers
                    select p;
            return View(datas);
        }
        public IActionResult Delete(int? id)
        {
            DbShizukuDemoContext db = new DbShizukuDemoContext();
            TMember x = db.TMembers.FirstOrDefault(p => p.FId == id);
            if (x != null)
            {
                x.FIsActive = false;
                db.SaveChanges();
            }
            return RedirectToAction("List");
        }

        public IActionResult Restore(int? id)
        {
            DbShizukuDemoContext db = new DbShizukuDemoContext();
            TMember x = db.TMembers.FirstOrDefault(p => p.FId == id);
            if (x != null)
            {
                x.FIsActive = true;
                db.SaveChanges();
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
            DbShizukuDemoContext db = new DbShizukuDemoContext();
            db.TMembers.Add(p.member);
            db.SaveChanges();

            int padLength = 3; //長度編號為:M0000 未來長度只改這行 2代表M00 3代表M000
            p.FMemberId = "M" + p.FId.ToString("D" + padLength);    //0001 0010 0100...解決格式化問題
            p.FAccount = p.FEmail;

            db.SaveChanges();
            return RedirectToAction("List");
        }

        public IActionResult Edit(int? id)
        {
            DbShizukuDemoContext db = new DbShizukuDemoContext();
            TMember x = db.TMembers.FirstOrDefault(p => p.FId == id);
            if (x == null)
                return RedirectToAction("List");
            return View(x);
        }
        [HttpPost]
        public IActionResult Edit(TMember uiCustomer)
        {
            DbShizukuDemoContext db = new DbShizukuDemoContext();
            TMember dbCustomer = db.TMembers.FirstOrDefault(p => p.FId == uiCustomer.FId);
            if (dbCustomer != null)
            {
                dbCustomer.FName = uiCustomer.FName;
                dbCustomer.FEmail = uiCustomer.FEmail;
                dbCustomer.FPassword = uiCustomer.FPassword;
                dbCustomer.FPhone = uiCustomer.FPhone;
                db.SaveChanges();
            }
            return RedirectToAction("List");
        }
    }
}
