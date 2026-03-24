using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminController : Controller
    {
        [HttpGet("")]
        [HttpGet("Dashboard")]
        public IActionResult Index()
        {
            var isAuth = User?.Identity?.IsAuthenticated;
            var role = User?.FindFirst(ClaimTypes.Role)?.Value;

            if (isAuth != true || role != "ADMIN")
                return RedirectToAction("Index", "Home");

            return View("~/Views/Admin/AdminDashboard.cshtml");
        }

        [HttpGet("Debug")]
        public IActionResult Debug()
        {
            var claims = User?.Claims?.Select(c => new { c.Type, c.Value }).ToList();
            return Json(new
            {
                isAuthenticated = User?.Identity?.IsAuthenticated,
                role = User?.FindFirst(ClaimTypes.Role)?.Value,
                username = User?.FindFirst(ClaimTypes.Name)?.Value,
                claims
            });
        }
    }
}