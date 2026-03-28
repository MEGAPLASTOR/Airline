using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminCityController : AdminBaseController
    {
        public AdminCityController(DataContext context) : base(context) { }

        [HttpGet("ManageCity")]
        public async Task<IActionResult> ManageCity()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var cities = await _context.Cities
                .OrderBy(x => x.CityName)
                .ToListAsync();

            return View("~/Views/Admin/ManageCity.cshtml", cities);
        }
        [HttpGet("GetCity/{id}")]
        public async Task<IActionResult> GetCity(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            var city = await _context.Cities.FindAsync(id);
            if (city == null) return NotFound();

            return Json(new { city.CityName, city.Country });
        }

        [HttpPost("CreateCity")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCity([FromForm] string cityName, [FromForm] string country)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            if (string.IsNullOrWhiteSpace(cityName) || string.IsNullOrWhiteSpace(country))
                return Json(new { success = false, message = "City Name and Country are required." });

            var city = new Cities { CityName = cityName.Trim(), Country = country.Trim() };
            _context.Cities.Add(city);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost("EditCity")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCity([FromForm] int id, [FromForm] string cityName, [FromForm] string country)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            if (string.IsNullOrWhiteSpace(cityName) || string.IsNullOrWhiteSpace(country))
                return Json(new { success = false, message = "City Name and Country are required." });

            var city = await _context.Cities.FindAsync(id);
            if (city == null) return Json(new { success = false, message = "City not found." });

            city.CityName = cityName.Trim();
            city.Country = country.Trim();

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("DeleteCity")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCity([FromForm] int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var city = await _context.Cities.FindAsync(id);
            if (city == null) return Json(new { success = false, message = "City not found." });

            var isUsedInRoutes = await _context.Routes
                .AnyAsync(r => r.DepartureCity == id || r.ArrivalCity == id);

            if (isUsedInRoutes)
            {
                return Json(new { success = false, message = "Cannot delete city because it is actively used in registered flight routes." });
            }

            _context.Cities.Remove(city);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
