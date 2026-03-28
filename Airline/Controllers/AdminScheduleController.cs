using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminScheduleController : AdminBaseController
    {
        public AdminScheduleController(DataContext context) : base(context) { }

        [HttpGet("FlightSchedules")]
        public async Task<IActionResult> FlightSchedules()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var schedules = await _context.FlightSchedules
                .Include(x => x.Flight)
                .Include(x => x.Flight.Route.DepartureCityNavigation) // Preload for better UI
                .Include(x => x.Flight.Route.ArrivalCityNavigation)
                .OrderByDescending(x => x.ScheduleId)
                .ToListAsync();

            ViewBag.Flights = await _context.Flights
                .Include(f => f.Route.DepartureCityNavigation)
                .Include(f => f.Route.ArrivalCityNavigation)
                .OrderBy(f => f.FlightNumber)
                .ToListAsync();

            return View("~/Views/Admin/FlightSchedules.cshtml", schedules);
        }

        [HttpGet("FlightReschedule")]
        public async Task<IActionResult> FlightReschedule()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var schedules = await _context.FlightSchedules
                .Include(x => x.Flight)
                .Include(x => x.Flight.Route.DepartureCityNavigation)
                .Include(x => x.Flight.Route.ArrivalCityNavigation)
                .OrderByDescending(x => x.ScheduleId)
                .ToListAsync();

            return View("~/Views/Admin/FlightReschedule.cshtml", schedules);
        }

        [HttpGet("GetSchedule/{id}")]
        public async Task<IActionResult> GetSchedule(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            var s = await _context.FlightSchedules.FindAsync(id);
            if (s == null) return NotFound();
            return Json(new { s.ScheduleId, s.FlightId, s.DepartureTime, s.ArrivalTime, s.TotalSeats, s.AvailableSeats, s.Status });
        }

        [HttpPost("CreateSchedule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSchedule([FromForm] int flightId, [FromForm] DateTime departureTime, [FromForm] DateTime arrivalTime, [FromForm] int totalSeats, [FromForm] string status)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            if (flightId == 0) return Json(new { success = false, message = "Flight is required." });
            if (departureTime >= arrivalTime) return Json(new { success = false, message = "Arrival time must be after departure time." });

            var s = new FlightSchedule
            {
                FlightId = flightId,
                DepartureTime = departureTime,
                ArrivalTime = arrivalTime,
                TotalSeats = totalSeats,
                AvailableSeats = totalSeats, // Available equals total on creation
                Status = string.IsNullOrWhiteSpace(status) ? "SCHEDULED" : status
            };

            _context.FlightSchedules.Add(s);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("EditSchedule/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSchedule(int id, [FromForm] int flightId, [FromForm] DateTime departureTime, [FromForm] DateTime arrivalTime, [FromForm] int totalSeats, [FromForm] int availableSeats, [FromForm] string status)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var s = await _context.FlightSchedules.FindAsync(id);
            if (s == null) return Json(new { success = false, message = "Schedule not found." });

            if (flightId == 0) return Json(new { success = false, message = "Flight is required." });
            if (departureTime >= arrivalTime) return Json(new { success = false, message = "Arrival time must be after departure time." });
            if (availableSeats < 0 || availableSeats > totalSeats) return Json(new { success = false, message = "Invalid available seats." });

            s.FlightId = flightId;
            s.DepartureTime = departureTime;
            s.ArrivalTime = arrivalTime;
            s.TotalSeats = totalSeats;
            s.AvailableSeats = availableSeats;
            s.Status = status;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("DeleteSchedule/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var s = await _context.FlightSchedules.Include(x => x.Bookings).FirstOrDefaultAsync(x => x.ScheduleId == id);
            if (s == null) return Json(new { success = false, message = "Schedule not found." });

            if (s.Bookings.Any()) return Json(new { success = false, message = "Cannot delete schedule with active bookings." });

            _context.FlightSchedules.Remove(s);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost("ProcessReschedule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReschedule([FromForm] int id, [FromForm] DateTime newDepartureTime, [FromForm] DateTime newArrivalTime, [FromForm] string status)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var s = await _context.FlightSchedules.FindAsync(id);
            if (s == null) return Json(new { success = false, message = "Schedule not found." });

            if (newDepartureTime >= newArrivalTime) return Json(new { success = false, message = "Arrival time must be after departure time." });

            s.DepartureTime = newDepartureTime;
            s.ArrivalTime = newArrivalTime;
            s.Status = string.IsNullOrWhiteSpace(status) ? "DELAYED" : status;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("CancelFlightSchedule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelFlightSchedule([FromForm] int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var s = await _context.FlightSchedules.FindAsync(id);
            if (s == null) return Json(new { success = false, message = "Schedule not found." });

            s.Status = "CANCELLED";

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
