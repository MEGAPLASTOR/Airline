using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminRouteController : AdminBaseController
    {
        public AdminRouteController(DataContext context) : base(context) { }

        [HttpGet("ManageRoutes")]
        public async Task<IActionResult> ManageRoutes()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var routes = await _context.Routes
                .Include(x => x.DepartureCityNavigation)
                .Include(x => x.ArrivalCityNavigation)
                .ToListAsync();

            return View("~/Views/Admin/ManageRoutes.cshtml", routes);
        }
    }
}
