using Airline.Models;
using Microsoft.AspNetCore.Mvc;

namespace Airline.Controllers
{
    public abstract class AdminBaseController : Controller
    {
        protected readonly DataContext _context;

        protected AdminBaseController(DataContext context)
        {
            _context = context;
        }

        protected bool IsAdmin()
        {
            var isAuth = User?.Identity?.IsAuthenticated == true;
            var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return isAuth && string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);
        }

        protected IActionResult RedirectIfNotAdmin()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}
