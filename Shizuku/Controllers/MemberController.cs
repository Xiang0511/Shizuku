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
            IEnumerable<TMember> datas = null;
            datas = from p in db.TMembers
                    select p;
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
    }
}
