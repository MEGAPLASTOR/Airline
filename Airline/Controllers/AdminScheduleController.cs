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

        // =========================
        // Flight Seats Management
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
                .Include(x => x.Bookings)
                    .ThenInclude(t => t.Tickets)
                        .ThenInclude(t => t.Passenger)
                .FirstOrDefaultAsync(x => x.ScheduleId == id.Value);

            if (schedule == null) return NotFound();

            return View("~/Views/Admin/FlightSeats.cshtml", schedule);
        }

        [HttpGet("GetSeatDetails")]
        public async Task<IActionResult> GetSeatDetails(int scheduleId, string seatNumber)
        {
            if (!IsAdmin()) return Unauthorized();

            var ticket = await _context.Tickets
                .Include(x => x.Passenger)
                .Include(x => x.Booking)
                    .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(x => x.Booking.ScheduleId == scheduleId && x.SeatNumber == seatNumber);

            if (ticket == null) return NotFound();

            return Json(new
            {
                success = true,
                seatNumber = ticket.SeatNumber,
                passengerName = ticket.Passenger?.FullName ?? "Unknown",
                passengerType = ticket.Passenger?.PassengerType ?? "N/A",
                status = ticket.Status,
                bookingDate = ticket.Booking?.BookingDate?.ToString("dd/MM/yyyy HH:mm") ?? "N/A",
                username = ticket.Booking?.User?.Username ?? "Guest"
            });
        }

        [HttpPost("ToggleSeatStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSeatStatus(int scheduleId, string seatNumber)
        {
            if (!IsAdmin()) return Unauthorized();

            var schedule = await _context.FlightSchedules.FirstOrDefaultAsync(x => x.ScheduleId == scheduleId);
            if (schedule == null) return Json(new { success = false, message = "Schedule not found" });

            // Check if there is an existing ticket
            var existingTicket = await _context.Tickets
                .Include(x => x.Booking)
                .FirstOrDefaultAsync(x => x.Booking.ScheduleId == scheduleId && x.SeatNumber == seatNumber);

            if (existingTicket != null)
            {
                if (existingTicket.Status == "BLOCKED")
                {
                    // Unblock: remove the placeholder ticket
                    _context.Tickets.Remove(existingTicket);
                    
                    schedule.AvailableSeats++;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, action = "unblocked" });
                }
                else
                {
                    return Json(new { success = false, message = "Cannot toggle status of an active reservation." });
                }
            }
            else
            {
                // Block: create a placeholder booking and ticket
                var systemUser = await _context.Users.FirstOrDefaultAsync(x => x.Role == "ADMIN");
                if (systemUser == null) return Json(new { success = false, message = "No admin user found for blocking." });

                var booking = new Booking
                {
                    UserId = systemUser.UserId,
                    ScheduleId = scheduleId,
                    BookingDate = DateTime.Now,
                    BookingType = "ADMIN_BLOCK",
                    Status = "BLOCKED"
                };
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                var ticket = new Ticket
                {
                    BookingId = booking.BookingId,
                    SeatNumber = seatNumber,
                    Status = "BLOCKED",
                    ClassId = (await _context.TicketClasses.FirstOrDefaultAsync())?.ClassId ?? 1
                };
                
                var passenger = new Passenger
                {
                    BookingId = booking.BookingId,
                    FullName = "ADMIN BLOCK",
                    PassengerType = "SYSTEM"
                };
                _context.Passengers.Add(passenger);
                await _context.SaveChangesAsync();

                ticket.PassengerId = passenger.PassengerId;
                _context.Tickets.Add(ticket);

                schedule.AvailableSeats--;
                await _context.SaveChangesAsync();
                return Json(new { success = true, action = "blocked" });
            }
        }
    }
}
