using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminFlightController : AdminBaseController
    {
        public AdminFlightController(DataContext context) : base(context) { }

        [HttpGet("ManageFlights")]
        public async Task<IActionResult> ManageFlights()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var flights = await _context.Flights
                .Include(x => x.Route)
                .OrderByDescending(x => x.FlightId)
                .ToListAsync();

            ViewBag.Routes = await _context.Routes
                .Include(r => r.DepartureCityNavigation)
                .Include(r => r.ArrivalCityNavigation)
                .ToListAsync();

            return View("~/Views/Admin/ManageFlights.cshtml", flights);
        }

        [HttpGet("GetFlight/{id}")]
        public async Task<IActionResult> GetFlight(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            var f = await _context.Flights.FindAsync(id);
            if (f == null) return NotFound();

            return Json(new { f.FlightId, f.FlightNumber, f.RouteId });
        }

        [HttpPost("CreateFlight")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFlight([FromForm] string flightNumber, [FromForm] int routeId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            if (string.IsNullOrWhiteSpace(flightNumber) || routeId == 0)
                return Json(new { success = false, message = "Flight number and route are required." });

            var exists = await _context.Flights.AnyAsync(f => f.FlightNumber == flightNumber.Trim());
            if (exists) return Json(new { success = false, message = "Flight number already exists." });

            var flight = new Flight { FlightNumber = flightNumber.Trim().ToUpper(), RouteId = routeId };
            _context.Flights.Add(flight);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost("EditFlight/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFlight(int id, [FromForm] string flightNumber, [FromForm] int routeId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var flight = await _context.Flights.FindAsync(id);
            if (flight == null) return Json(new { success = false, message = "Flight not found." });

            if (string.IsNullOrWhiteSpace(flightNumber) || routeId == 0)
                return Json(new { success = false, message = "Flight number and route are required." });

            var exists = await _context.Flights.AnyAsync(f => f.FlightId != id && f.FlightNumber == flightNumber.Trim());
            if (exists) return Json(new { success = false, message = "Flight number already exists." });

            flight.FlightNumber = flightNumber.Trim().ToUpper();
            flight.RouteId = routeId;
            
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("DeleteFlight/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFlight(int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var flight = await _context.Flights.Include(x => x.FlightSchedules).FirstOrDefaultAsync(x => x.FlightId == id);
            if (flight == null) return Json(new { success = false, message = "Flight not found." });

            if (flight.FlightSchedules.Any()) 
                return Json(new { success = false, message = "Cannot delete flight because it has active schedules." });

            _context.Flights.Remove(flight);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
