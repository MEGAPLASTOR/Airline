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
                .Include(x => x.Flights)
                .ToListAsync();

            ViewBag.Cities = await _context.Cities.OrderBy(c => c.CityName).ToListAsync();

            return View("~/Views/Admin/ManageRoutes.cshtml", routes);
        }

        [HttpGet("GetRoute/{id}")]
        public async Task<IActionResult> GetRoute(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            var route = await _context.Routes.FindAsync(id);
            if (route == null) return NotFound();

            return Json(new { route.DepartureCity, route.ArrivalCity });
        }

        [HttpPost("CreateRoute")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoute([FromForm] int departureCity, [FromForm] int arrivalCity)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            if (departureCity == 0 || arrivalCity == 0)
                return Json(new { success = false, message = "Please select both cities." });

            if (departureCity == arrivalCity)
                return Json(new { success = false, message = "Departure and arrival cities cannot be the same." });

            var exists = await _context.Routes.AnyAsync(r => r.DepartureCity == departureCity && r.ArrivalCity == arrivalCity);
            if (exists)
                return Json(new { success = false, message = "This route already exists." });

            var route = new Airline.Models.Route { DepartureCity = departureCity, ArrivalCity = arrivalCity };
            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost("EditRoute")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoute([FromForm] int id, [FromForm] int departureCity, [FromForm] int arrivalCity)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var route = await _context.Routes.FindAsync(id);
            if (route == null) return Json(new { success = false, message = "Route not found." });

            if (departureCity == 0 || arrivalCity == 0)
                return Json(new { success = false, message = "Please select both cities." });

            if (departureCity == arrivalCity)
                return Json(new { success = false, message = "Departure and arrival cities cannot be the same." });

            var exists = await _context.Routes.AnyAsync(r => r.RouteId != id && r.DepartureCity == departureCity && r.ArrivalCity == arrivalCity);
            if (exists)
                return Json(new { success = false, message = "This route already exists." });

            route.DepartureCity = departureCity;
            route.ArrivalCity = arrivalCity;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("DeleteRoute")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoute([FromForm] int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var route = await _context.Routes.Include(r => r.Flights).FirstOrDefaultAsync(r => r.RouteId == id);
            if (route == null) return Json(new { success = false, message = "Route not found." });

            if (route.Flights.Any())
                return Json(new { success = false, message = "Cannot delete route with active flights." });

            _context.Routes.Remove(route);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
