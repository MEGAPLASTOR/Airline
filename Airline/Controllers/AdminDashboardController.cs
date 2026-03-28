using Airline.Models;
using Microsoft.AspNetCore.Mvc;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminDashboardController : AdminBaseController
    {
        public AdminDashboardController(DataContext context) : base(context) { }

        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();
            return View("~/Views/Admin/AdminDashboard.cshtml");
        }

        [HttpGet("AdminDashboard")]
        public IActionResult AdminDashboard()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();
            return View("~/Views/Admin/AdminDashboard.cshtml");
        }
    }
}
