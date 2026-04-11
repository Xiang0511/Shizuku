using Microsoft.AspNetCore.Mvc;

namespace Shizuku.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Order()
        {
            return View();
        }
    }
}
