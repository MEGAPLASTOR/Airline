using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    public class BookingController : Controller
    {
        private readonly DataContext _context;

        public BookingController(DataContext context)
        {
            _context = context;
        }

        public IActionResult BookFlight(string from, string to, DateTime? date)
        {
            var query = _context.FlightSchedules
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.ArrivalCityNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(from))
                query = query.Where(s => s.Flight.Route.DepartureCityNavigation.CityName.Contains(from));

            if (!string.IsNullOrEmpty(to))
                query = query.Where(s => s.Flight.Route.ArrivalCityNavigation.CityName.Contains(to));

            if (date.HasValue)
                query = query.Where(s => s.DepartureTime.Date == date.Value.Date);

            return View(query.ToList());
        }

        public IActionResult SelectSeat(int scheduleId)
        {
            var schedule = _context.FlightSchedules
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Seats)
                        .ThenInclude(se => se.Class)
                .FirstOrDefault(s => s.ScheduleId == scheduleId);

            if (schedule == null) return NotFound();

            // Find already booked seats for this schedule
            var bookedSeats = _context.Tickets
                .Where(t => t.Booking.ScheduleId == scheduleId)
                .Select(t => t.SeatNumber)
                .ToList();

            ViewBag.BookedSeats = bookedSeats;
            return View(schedule);
        }

        [HttpPost]
        public IActionResult ConfirmSelection(int scheduleId, string seatNumber)
        {
            // Logic for confirming booking (stub for now)
            return Json(new { success = true, message = $"Selected seat {seatNumber} for schedule {scheduleId}" });
        }
    }
}
