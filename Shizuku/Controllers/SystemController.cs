using Microsoft.AspNetCore.Mvc;

namespace Shizuku.Controllers
{
    public class SystemController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
