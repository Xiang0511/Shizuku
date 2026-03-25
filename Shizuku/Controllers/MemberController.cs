using Microsoft.AspNetCore.Mvc;

namespace Shizuku.Controllers
{
    public class MemberController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
