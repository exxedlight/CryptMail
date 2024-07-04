using Microsoft.AspNetCore.Mvc;

namespace CryptMail.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Wellcome()
        {
            return View();
        }
    }
}
