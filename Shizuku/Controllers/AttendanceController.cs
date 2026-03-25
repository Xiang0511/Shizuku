using Microsoft.AspNetCore.Mvc;

namespace Shizuku.Controllers
{
    public class AttendanceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
