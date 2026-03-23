using Microsoft.AspNetCore.Mvc;

namespace Airline.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
