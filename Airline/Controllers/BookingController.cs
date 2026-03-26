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
        public async Task<IActionResult> ConfirmSelection(int scheduleId, string seatNumber)
        {
            var schedule = await _context.FlightSchedules
                .Include(s => s.Flight)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

            if (schedule == null) return Json(new { success = false, message = "Schedule not found." });

            // Ensure seat exists and is active
            var seat = await _context.Seats.FirstOrDefaultAsync(s => s.FlightId == schedule.FlightId && s.SeatNumber == seatNumber);
            if (seat == null || !seat.IsActive)
                return Json(new { success = false, message = "Seat is not available." });

            // Check if already booked
            var isBooked = await _context.Tickets.AnyAsync(t => t.Booking.ScheduleId == scheduleId && t.SeatNumber == seatNumber);
            if (isBooked)
                return Json(new { success = false, message = "Seat is already booked." });

            try
            {
                // 1. Create Booking
                var booking = new Booking
                {
                    UserId = 1, // Placeholder for logged-in user
                    ScheduleId = scheduleId,
                    BookingType = "ONEWAY",
                    BookingDate = DateTime.Now,
                    Status = "CONFIRMED"
                };
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // 2. Create Ticket
                var ticket = new Ticket
                {
                    BookingId = booking.BookingId,
                    PassengerId = 1, // Placeholder for passenger
                    ClassId = seat.ClassId,
                    SeatNumber = seatNumber,
                    Status = "ACTIVE"
                };
                _context.Tickets.Add(ticket);

                // 3. Update Available Seats
                if (schedule.AvailableSeats > 0)
                    schedule.AvailableSeats--;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Successfully booked seat {seatNumber}!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}
