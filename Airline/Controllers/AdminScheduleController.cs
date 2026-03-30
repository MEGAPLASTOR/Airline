using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Airline.Services;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminScheduleController : AdminBaseController
    {
        private readonly SeatService _seatService;

        public AdminScheduleController(DataContext context, SeatService seatService) : base(context) 
        {
            _seatService = seatService;
        }

        [HttpGet("FlightSchedules")]
        public async Task<IActionResult> FlightSchedules()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var schedules = await _context.FlightSchedules
                .Include(x => x.Flight)
                .Include(x => x.Flight.Route.DepartureCityNavigation)
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
                AvailableSeats = totalSeats,
                Status = string.IsNullOrWhiteSpace(status) ? "SCHEDULED" : status
            };

            _context.FlightSchedules.Add(s);
            await _context.SaveChangesAsync();

            // GENERATE SEATS for the 4 cabins
            await _seatService.GenerateSeatsAsync(s.ScheduleId);

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

            int diff = totalSeats - (s.TotalSeats ?? 180);
            s.FlightId = flightId;
            s.DepartureTime = departureTime;
            s.ArrivalTime = arrivalTime;
            s.TotalSeats = totalSeats;
            s.AvailableSeats = Math.Max(0, (s.AvailableSeats ?? 180) + diff);
            s.Status = status;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // =========================
        // Flight Seats Management (Refactored for Seat entity)
        // =========================
        [HttpGet("FlightSeats/{id?}")]
        public async Task<IActionResult> FlightSeats(int? id)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            if (id == null)
            {
                var schedules = await _context.FlightSchedules
                    .Include(x => x.Flight)
                    .OrderByDescending(x => x.DepartureTime)
                    .ToListAsync();
                return View("~/Views/Admin/FlightSeats.cshtml", schedules);
            }

            var schedule = await _context.FlightSchedules
                .Include(x => x.Flight)
                .Include(x => x.Seats)
                    .ThenInclude(s => s.Class)
                .FirstOrDefaultAsync(x => x.ScheduleId == id.Value);

            if (schedule == null) return NotFound();

            return View("~/Views/Admin/FlightSeats.cshtml", schedule);
        }

        [HttpGet("GetSeatDetails")]
        public async Task<IActionResult> GetSeatDetails(int scheduleId, string seatNumber)
        {
            if (!IsAdmin()) return Unauthorized();

            var seat = await _context.Seats
                .Include(s => s.Class)
                .Include(s => s.Tickets)
                    .ThenInclude(t => t.Passenger)
                .Include(s => s.Tickets)
                    .ThenInclude(t => t.Booking)
                        .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(x => x.ScheduleId == scheduleId && x.SeatNumber == seatNumber);

            if (seat == null) return NotFound();

            var ticket = seat.Tickets.OrderByDescending(t => t.TicketId).FirstOrDefault();

            return Json(new
            {
                success = true,
                seatNumber = seat.SeatNumber,
                className = seat.Class.ClassName,
                status = seat.SeatStatus,
                passengerName = ticket?.Passenger?.FullName ?? "N/A",
                username = ticket?.Booking?.User?.Username ?? "N/A",
                bookingDate = ticket?.Booking?.BookingDate?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"
            });
        }

        [HttpPost("ToggleSeatStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSeatStatus(int scheduleId, string seatNumber)
        {
            if (!IsAdmin()) return Unauthorized();

            var seat = await _context.Seats.FirstOrDefaultAsync(x => x.ScheduleId == scheduleId && x.SeatNumber == seatNumber);
            if (seat == null) return Json(new { success = false, message = "Seat not found" });

            if (seat.SeatStatus == "AVAILABLE")
            {
                seat.SeatStatus = "BLOCKED";
                await _context.SaveChangesAsync();
                return Json(new { success = true, action = "blocked" });
            }
            else if (seat.SeatStatus == "BLOCKED")
            {
                seat.SeatStatus = "AVAILABLE";
                await _context.SaveChangesAsync();
                return Json(new { success = true, action = "unblocked" });
            }
            else
            {
                return Json(new { success = false, message = "Cannot toggle status of an active reservation." });
            }
        }
    }
}
