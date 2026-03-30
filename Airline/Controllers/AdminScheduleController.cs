using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminScheduleController : AdminBaseController
    {
        private static readonly HashSet<string> ScheduleStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "SCHEDULED",
            "DELAYED",
            "CANCELLED",
            "COMPLETED"
        };

        private static readonly HashSet<string> RescheduleStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "SCHEDULED",
            "DELAYED"
        };

        public AdminScheduleController(DataContext context) : base(context)
        {
        }

        [HttpGet("FlightSchedules")]
        public async Task<IActionResult> FlightSchedules(string? search, string? status, string? dateFrom, string? dateTo)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var normalizedSearch = search?.Trim();
            var normalizedStatus = NormalizeFilterStatus(status);
            var query = BuildScheduleQuery();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                query = query.Where(s =>
                    s.Flight.FlightNumber.Contains(normalizedSearch) ||
                    s.Flight.Route.DepartureCityNavigation.CityName.Contains(normalizedSearch) ||
                    s.Flight.Route.ArrivalCityNavigation.CityName.Contains(normalizedSearch));
            }

            if (!string.Equals(normalizedStatus, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(s => s.Status == normalizedStatus);
            }

            if (DateOnly.TryParse(dateFrom, out var fromDate))
            {
                var from = fromDate.ToDateTime(TimeOnly.MinValue);
                query = query.Where(s => s.DepartureTime >= from);
                ViewBag.DateFrom = fromDate.ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.DateFrom = dateFrom?.Trim() ?? string.Empty;
            }

            if (DateOnly.TryParse(dateTo, out var toDate))
            {
                var to = toDate.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(s => s.DepartureTime <= to);
                ViewBag.DateTo = toDate.ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.DateTo = dateTo?.Trim() ?? string.Empty;
            }

            ViewBag.Search = normalizedSearch ?? string.Empty;
            ViewBag.StatusFilter = normalizedStatus;
            ViewBag.Flights = await LoadFlightsAsync();

            var schedules = await query
                .OrderByDescending(s => s.ScheduleId)
                .ToListAsync();

            return View("~/Views/Admin/FlightSchedules.cshtml", schedules);
        }

        [HttpGet("FlightReschedule")]
        public async Task<IActionResult> FlightReschedule(string? search, string? status, string? filterDate)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var normalizedSearch = search?.Trim();
            var normalizedStatus = NormalizeFilterStatus(status);
            var query = BuildScheduleQuery();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                query = query.Where(s =>
                    s.Flight.FlightNumber.Contains(normalizedSearch) ||
                    s.Flight.Route.DepartureCityNavigation.CityName.Contains(normalizedSearch) ||
                    s.Flight.Route.ArrivalCityNavigation.CityName.Contains(normalizedSearch));
            }

            if (!string.Equals(normalizedStatus, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(s => s.Status == normalizedStatus);
            }

            if (DateOnly.TryParse(filterDate, out var selectedDate))
            {
                var from = selectedDate.ToDateTime(TimeOnly.MinValue);
                var to = selectedDate.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(s => s.DepartureTime >= from && s.DepartureTime <= to);
                ViewBag.FilterDate = selectedDate.ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.FilterDate = filterDate?.Trim() ?? string.Empty;
            }

            ViewBag.Search = normalizedSearch ?? string.Empty;
            ViewBag.StatusFilter = normalizedStatus;
            ViewBag.Flights = await LoadFlightsAsync();

            var schedules = await query
                .OrderByDescending(s => s.DepartureTime)
                .ToListAsync();

            return View("~/Views/Admin/FlightReschedule.cshtml", schedules);
        }

        [HttpGet("GetSchedule/{id}")]
        public async Task<IActionResult> GetSchedule(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            var schedule = await _context.FlightSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null) return NotFound();

            return Json(new
            {
                schedule.ScheduleId,
                schedule.FlightId,
                DepartureTime = schedule.DepartureTime.ToString("yyyy-MM-ddTHH:mm"),
                ArrivalTime = schedule.ArrivalTime.ToString("yyyy-MM-ddTHH:mm"),
                TotalSeats = schedule.TotalSeats ?? 0,
                AvailableSeats = schedule.AvailableSeats ?? 0,
                Status = NormalizeStatus(schedule.Status, "SCHEDULED", ScheduleStatuses)
            });
        }

        [HttpPost("CreateSchedule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSchedule(
            [FromForm] int flightId,
            [FromForm] DateTime? departureTime,
            [FromForm] DateTime? arrivalTime,
            [FromForm] int totalSeats,
            [FromForm] string? status)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            if (flightId <= 0)
                return Json(new { success = false, message = "Flight is required." });

            if (!departureTime.HasValue || !arrivalTime.HasValue)
                return Json(new { success = false, message = "Departure time and arrival time are required." });

            if (departureTime.Value >= arrivalTime.Value)
                return Json(new { success = false, message = "Arrival time must be after departure time." });

            if (totalSeats <= 0)
                return Json(new { success = false, message = "Total seats must be greater than zero." });

            var flightExists = await _context.Flights.AnyAsync(f => f.FlightId == flightId);
            if (!flightExists)
                return Json(new { success = false, message = "Selected flight does not exist." });

            var duplicateSchedule = await _context.FlightSchedules.AnyAsync(s =>
                s.FlightId == flightId &&
                s.DepartureTime == departureTime.Value);

            if (duplicateSchedule)
            {
                return Json(new
                {
                    success = false,
                    message = "A schedule for this flight at the selected departure time already exists."
                });
            }

            var schedule = new FlightSchedule
            {
                FlightId = flightId,
                DepartureTime = departureTime.Value,
                ArrivalTime = arrivalTime.Value,
                TotalSeats = totalSeats,
                AvailableSeats = totalSeats,
                Status = NormalizeStatus(status, "SCHEDULED", ScheduleStatuses)
            };

            _context.FlightSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost("EditSchedule/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSchedule(
            int id,
            [FromForm] int flightId,
            [FromForm] DateTime? departureTime,
            [FromForm] DateTime? arrivalTime,
            [FromForm] int totalSeats,
            [FromForm] int availableSeats,
            [FromForm] string? status)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var schedule = await _context.FlightSchedules.FirstOrDefaultAsync(s => s.ScheduleId == id);
            if (schedule == null)
                return Json(new { success = false, message = "Schedule not found." });

            if (flightId <= 0)
                return Json(new { success = false, message = "Flight is required." });

            if (!departureTime.HasValue || !arrivalTime.HasValue)
                return Json(new { success = false, message = "Departure time and arrival time are required." });

            if (departureTime.Value >= arrivalTime.Value)
                return Json(new { success = false, message = "Arrival time must be after departure time." });

            if (totalSeats <= 0)
                return Json(new { success = false, message = "Total seats must be greater than zero." });

            var flightExists = await _context.Flights.AnyAsync(f => f.FlightId == flightId);
            if (!flightExists)
                return Json(new { success = false, message = "Selected flight does not exist." });

            var hasBookings = await _context.Bookings.AnyAsync(b => b.ScheduleId == id);
            if (hasBookings && schedule.FlightId != flightId)
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot change the flight of a schedule that already has bookings."
                });
            }

            var occupiedSeatCount = await CountActiveTicketsAsync(id);
            if (totalSeats < occupiedSeatCount)
            {
                return Json(new
                {
                    success = false,
                    message = $"Total seats cannot be less than the {occupiedSeatCount} seats already in use."
                });
            }

            var maxAvailableSeats = totalSeats - occupiedSeatCount;
            if (availableSeats < 0 || availableSeats > totalSeats || availableSeats > maxAvailableSeats)
            {
                return Json(new
                {
                    success = false,
                    message = $"Available seats must be between 0 and {maxAvailableSeats}."
                });
            }

            var duplicateSchedule = await _context.FlightSchedules.AnyAsync(s =>
                s.ScheduleId != id &&
                s.FlightId == flightId &&
                s.DepartureTime == departureTime.Value);

            if (duplicateSchedule)
            {
                return Json(new
                {
                    success = false,
                    message = "A schedule for this flight at the selected departure time already exists."
                });
            }

            schedule.FlightId = flightId;
            schedule.DepartureTime = departureTime.Value;
            schedule.ArrivalTime = arrivalTime.Value;
            schedule.TotalSeats = totalSeats;
            schedule.AvailableSeats = availableSeats;
            schedule.Status = NormalizeStatus(status, "SCHEDULED", ScheduleStatuses);

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("DeleteSchedule/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var schedule = await _context.FlightSchedules
                .Include(s => s.Bookings)
                .Include(s => s.TicketPrices)
                .Include(s => s.Seats)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null)
                return Json(new { success = false, message = "Schedule not found." });

            if (schedule.Bookings.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot delete schedule because it already has bookings."
                });
            }

            if (schedule.TicketPrices.Any())
            {
                _context.TicketPrices.RemoveRange(schedule.TicketPrices);
            }

            if (schedule.Seats.Any())
            {
                _context.Seats.RemoveRange(schedule.Seats);
            }

            _context.FlightSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost("ProcessReschedule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReschedule(
            [FromForm] int id,
            [FromForm] DateTime? newDepartureTime,
            [FromForm] DateTime? newArrivalTime,
            [FromForm] string? status)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var schedule = await _context.FlightSchedules.FirstOrDefaultAsync(s => s.ScheduleId == id);
            if (schedule == null)
                return Json(new { success = false, message = "Schedule not found." });

            if (!newDepartureTime.HasValue || !newArrivalTime.HasValue)
                return Json(new { success = false, message = "New departure and arrival times are required." });

            if (newDepartureTime.Value >= newArrivalTime.Value)
                return Json(new { success = false, message = "Arrival time must be after departure time." });

            if (string.Equals(schedule.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(schedule.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = false,
                    message = "Only scheduled or delayed flights can be rescheduled."
                });
            }

            schedule.DepartureTime = newDepartureTime.Value;
            schedule.ArrivalTime = newArrivalTime.Value;
            schedule.Status = NormalizeStatus(status, "DELAYED", RescheduleStatuses);

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("CancelFlightSchedule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelFlightSchedule([FromForm] int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var schedule = await _context.FlightSchedules.FirstOrDefaultAsync(s => s.ScheduleId == id);
            if (schedule == null)
                return Json(new { success = false, message = "Schedule not found." });

            if (string.Equals(schedule.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = false,
                    message = "Completed flights cannot be cancelled."
                });
            }

            schedule.Status = "CANCELLED";
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet("FlightSeats/{id?}")]
        public async Task<IActionResult> FlightSeats(int? id)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            if (id == null)
            {
                var schedules = await BuildScheduleQuery()
                    .OrderByDescending(s => s.DepartureTime)
                    .ToListAsync();

                return View("~/Views/Admin/FlightSeats.cshtml", schedules);
            }

            var schedule = await _context.FlightSchedules
                .AsNoTracking()
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.ArrivalCityNavigation)
                .Include(s => s.Bookings)
                    .ThenInclude(b => b.Tickets)
                        .ThenInclude(t => t.Passenger)
                .Include(s => s.Bookings)
                    .ThenInclude(b => b.Tickets)
                        .ThenInclude(t => t.Seat)
                .Include(s => s.Seats)
                .FirstOrDefaultAsync(s => s.ScheduleId == id.Value);

            if (schedule == null) return NotFound();

            return View("~/Views/Admin/FlightSeats.cshtml", schedule);
        }

        [HttpGet("GetSeatDetails")]
        public async Task<IActionResult> GetSeatDetails(int scheduleId, string seatNumber)
        {
            if (!IsAdmin()) return Unauthorized();

            if (string.IsNullOrWhiteSpace(seatNumber))
                return BadRequest();

            var normalizedSeatNumber = seatNumber.Trim().ToUpperInvariant();

            var ticket = await _context.Tickets
                .AsNoTracking()
                .Include(t => t.Passenger)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.User)
                .Include(t => t.Seat)
                .FirstOrDefaultAsync(t =>
                    t.Booking.ScheduleId == scheduleId &&
                    t.Seat != null &&
                    t.Seat.SeatNumber == normalizedSeatNumber);

            if (ticket == null) return NotFound();

            return Json(new
            {
                success = true,
                seatNumber = ticket.Seat?.SeatNumber ?? normalizedSeatNumber,
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

            if (string.IsNullOrWhiteSpace(seatNumber))
                return Json(new { success = false, message = "Seat number is required." });

            var normalizedSeatNumber = seatNumber.Trim().ToUpperInvariant();
            var schedule = await _context.FlightSchedules.FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
            if (schedule == null)
                return Json(new { success = false, message = "Schedule not found." });

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var seat = await _context.Seats
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId && s.SeatNumber == normalizedSeatNumber);

            var existingTicket = seat == null
                ? null
                : await _context.Tickets
                    .Include(t => t.Booking)
                    .FirstOrDefaultAsync(t => t.SeatId == seat.SeatId);

            if (existingTicket != null)
            {
                var isBlockedSeat = string.Equals(existingTicket.Status, "BLOCKED", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(existingTicket.Booking?.BookingType, "ADMIN_BLOCK", StringComparison.OrdinalIgnoreCase);

                if (!isBlockedSeat)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cannot toggle status of an active reservation."
                    });
                }

                var bookingId = existingTicket.BookingId;
                var passengerId = existingTicket.PassengerId;
                var hasOtherTickets = await _context.Tickets.AnyAsync(t =>
                    t.BookingId == bookingId && t.TicketId != existingTicket.TicketId);
                var hasOtherPassengers = await _context.Passengers.AnyAsync(p =>
                    p.BookingId == bookingId && p.PassengerId != passengerId);

                _context.Tickets.Remove(existingTicket);

                var passenger = await _context.Passengers.FindAsync(passengerId);
                if (passenger != null && !hasOtherPassengers)
                {
                    _context.Passengers.Remove(passenger);
                }

                if (!hasOtherTickets && !hasOtherPassengers)
                {
                    var booking = existingTicket.Booking ?? await _context.Bookings.FindAsync(bookingId);
                    if (booking != null)
                    {
                        _context.Bookings.Remove(booking);
                    }
                }

                if (seat != null)
                {
                    _context.Seats.Remove(seat);
                }

                schedule.AvailableSeats = Math.Min(
                    (schedule.AvailableSeats ?? 0) + 1,
                    schedule.TotalSeats ?? int.MaxValue);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, action = "unblocked" });
            }

            if (seat != null && string.Equals(seat.SeatStatus, "BLOCKED", StringComparison.OrdinalIgnoreCase))
            {
                _context.Seats.Remove(seat);
                schedule.AvailableSeats = Math.Min(
                    (schedule.AvailableSeats ?? 0) + 1,
                    schedule.TotalSeats ?? int.MaxValue);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, action = "unblocked" });
            }

            if ((schedule.AvailableSeats ?? 0) <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "No available seats are left to block."
                });
            }

            var systemUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Role == "ADMIN");
            if (systemUser == null)
            {
                return Json(new
                {
                    success = false,
                    message = "No admin user found for blocking."
                });
            }

            var classId = await ResolveSeatClassIdAsync();
            if (classId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "No ticket class is configured for seat blocking."
                });
            }

            if (seat == null)
            {
                seat = new Seat
                {
                    ScheduleId = scheduleId,
                    ClassId = classId,
                    SeatNumber = normalizedSeatNumber,
                    SeatStatus = "BLOCKED"
                };
                _context.Seats.Add(seat);
                await _context.SaveChangesAsync();
            }
            else
            {
                seat.ClassId = classId;
                seat.SeatStatus = "BLOCKED";
            }

            var blockedBooking = new Booking
            {
                UserId = systemUser.UserId,
                ScheduleId = scheduleId,
                BookingDate = DateTime.Now,
                BookingType = "ADMIN_BLOCK",
                Status = "BLOCKED"
            };
            _context.Bookings.Add(blockedBooking);
            await _context.SaveChangesAsync();

            var blockedPassenger = new Passenger
            {
                BookingId = blockedBooking.BookingId,
                FullName = "ADMIN BLOCK",
                PassengerType = "SYSTEM"
            };
            _context.Passengers.Add(blockedPassenger);
            await _context.SaveChangesAsync();

            var ticket = new Ticket
            {
                BookingId = blockedBooking.BookingId,
                PassengerId = blockedPassenger.PassengerId,
                ClassId = classId,
                SeatId = seat.SeatId,
                Status = "BLOCKED"
            };
            _context.Tickets.Add(ticket);

            schedule.AvailableSeats = Math.Max(0, (schedule.AvailableSeats ?? 0) - 1);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Json(new { success = true, action = "blocked" });
        }

        private IQueryable<FlightSchedule> BuildScheduleQuery()
        {
            return _context.FlightSchedules
                .AsNoTracking()
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.ArrivalCityNavigation);
        }

        private async Task<List<Flight>> LoadFlightsAsync()
        {
            return await _context.Flights
                .AsNoTracking()
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureCityNavigation)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalCityNavigation)
                .OrderBy(f => f.FlightNumber)
                .ToListAsync();
        }

        private async Task<int> CountActiveTicketsAsync(int scheduleId)
        {
            return await _context.Tickets.CountAsync(t =>
                t.Booking.ScheduleId == scheduleId &&
                t.Status != "CANCELLED");
        }

        private async Task<int> ResolveSeatClassIdAsync()
        {
            return await _context.TicketClasses
                .OrderBy(c => c.ClassId)
                .Select(c => c.ClassId)
                .FirstOrDefaultAsync();
        }

        private static string NormalizeFilterStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "ALL";
            }

            var normalized = status.Trim().ToUpperInvariant();
            return ScheduleStatuses.Contains(normalized) ? normalized : "ALL";
        }

        private static string NormalizeStatus(string? status, string fallback, ISet<string> allowedStatuses)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return fallback;
            }

            var normalized = status.Trim().ToUpperInvariant();
            return allowedStatuses.Contains(normalized) ? normalized : fallback;
        }
    }
}

//using Airline.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace Airline.Controllers
//{
//    [Route("Admin")]
//    public class AdminScheduleController : AdminBaseController
//    {
//        public AdminScheduleController(DataContext context) : base(context) { }

//        [HttpGet("FlightSchedules")]
//        public async Task<IActionResult> FlightSchedules()
//        {
//            if (!IsAdmin()) return RedirectIfNotAdmin();

//            var schedules = await _context.FlightSchedules
//                .Include(x => x.Flight)
//                .Include(x => x.Flight.Route.DepartureCityNavigation) // Preload for better UI
//                .Include(x => x.Flight.Route.ArrivalCityNavigation)
//                .OrderByDescending(x => x.ScheduleId)
//                .ToListAsync();

//            ViewBag.Flights = await _context.Flights
//                .Include(f => f.Route.DepartureCityNavigation)
//                .Include(f => f.Route.ArrivalCityNavigation)
//                .OrderBy(f => f.FlightNumber)
//                .ToListAsync();

//            return View("~/Views/Admin/FlightSchedules.cshtml", schedules);
//        }

//        [HttpGet("FlightReschedule")]
//        public async Task<IActionResult> FlightReschedule()
//        {
//            if (!IsAdmin()) return RedirectIfNotAdmin();

//            var schedules = await _context.FlightSchedules
//                .Include(x => x.Flight)
//                .Include(x => x.Flight.Route.DepartureCityNavigation)
//                .Include(x => x.Flight.Route.ArrivalCityNavigation)
//                .OrderByDescending(x => x.ScheduleId)
//                .ToListAsync();

//            return View("~/Views/Admin/FlightReschedule.cshtml", schedules);
//        }

//        [HttpGet("GetSchedule/{id}")]
//        public async Task<IActionResult> GetSchedule(int id)
//        {
//            if (!IsAdmin()) return Unauthorized();
//            var s = await _context.FlightSchedules.FindAsync(id);
//            if (s == null) return NotFound();
//            return Json(new { s.ScheduleId, s.FlightId, s.DepartureTime, s.ArrivalTime, s.TotalSeats, s.AvailableSeats, s.Status });
//        }

//        [HttpPost("CreateSchedule")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> CreateSchedule([FromForm] int flightId, [FromForm] DateTime departureTime, [FromForm] DateTime arrivalTime, [FromForm] int totalSeats, [FromForm] string status)
//        {
//            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

//            if (flightId == 0) return Json(new { success = false, message = "Flight is required." });
//            if (departureTime >= arrivalTime) return Json(new { success = false, message = "Arrival time must be after departure time." });

//            var s = new FlightSchedule
//            {
//                FlightId = flightId,
//                DepartureTime = departureTime,
//                ArrivalTime = arrivalTime,
//                TotalSeats = totalSeats,
//                AvailableSeats = totalSeats, // Available equals total on creation
//                Status = string.IsNullOrWhiteSpace(status) ? "SCHEDULED" : status
//            };

//            _context.FlightSchedules.Add(s);
//            await _context.SaveChangesAsync();
//            return Json(new { success = true });
//        }

//        [HttpPost("EditSchedule/{id}")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> EditSchedule(int id, [FromForm] int flightId, [FromForm] DateTime departureTime, [FromForm] DateTime arrivalTime, [FromForm] int totalSeats, [FromForm] int availableSeats, [FromForm] string status)
//        {
//            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

//            var s = await _context.FlightSchedules.FindAsync(id);
//            if (s == null) return Json(new { success = false, message = "Schedule not found." });

//            if (flightId == 0) return Json(new { success = false, message = "Flight is required." });
//            if (departureTime >= arrivalTime) return Json(new { success = false, message = "Arrival time must be after departure time." });
//            if (availableSeats < 0 || availableSeats > totalSeats) return Json(new { success = false, message = "Invalid available seats." });

//            s.FlightId = flightId;
//            s.DepartureTime = departureTime;
//            s.ArrivalTime = arrivalTime;
//            s.TotalSeats = totalSeats;
//            s.AvailableSeats = availableSeats;
//            s.Status = status;

//            await _context.SaveChangesAsync();
//            return Json(new { success = true });
//        }

//        [HttpPost("DeleteSchedule/{id}")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteSchedule(int id)
//        {
//            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

//            var s = await _context.FlightSchedules.Include(x => x.Bookings).FirstOrDefaultAsync(x => x.ScheduleId == id);
//            if (s == null) return Json(new { success = false, message = "Schedule not found." });

//            if (s.Bookings.Any()) return Json(new { success = false, message = "Cannot delete schedule with active bookings." });

//            _context.FlightSchedules.Remove(s);
//            await _context.SaveChangesAsync();

//            return Json(new { success = true });
//        }

//        [HttpPost("ProcessReschedule")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> ProcessReschedule([FromForm] int id, [FromForm] DateTime newDepartureTime, [FromForm] DateTime newArrivalTime, [FromForm] string status)
//        {
//            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

//            var s = await _context.FlightSchedules.FindAsync(id);
//            if (s == null) return Json(new { success = false, message = "Schedule not found." });

//            if (newDepartureTime >= newArrivalTime) return Json(new { success = false, message = "Arrival time must be after departure time." });

//            s.DepartureTime = newDepartureTime;
//            s.ArrivalTime = newArrivalTime;
//            s.Status = string.IsNullOrWhiteSpace(status) ? "DELAYED" : status;

//            await _context.SaveChangesAsync();
//            return Json(new { success = true });
//        }

//        [HttpPost("CancelFlightSchedule")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> CancelFlightSchedule([FromForm] int id)
//        {
//            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

//            var s = await _context.FlightSchedules.FindAsync(id);
//            if (s == null) return Json(new { success = false, message = "Schedule not found." });

//            s.Status = "CANCELLED";

//            await _context.SaveChangesAsync();
//            return Json(new { success = true });
//        }

//        // =========================
//        // Flight Seats Management
//        // =========================
//        [HttpGet("FlightSeats/{id?}")]
//        public async Task<IActionResult> FlightSeats(int? id)
//        {
//            if (!IsAdmin()) return RedirectIfNotAdmin();

//            if (id == null)
//            {
//                var schedules = await _context.FlightSchedules
//                    .Include(x => x.Flight)
//                    .OrderByDescending(x => x.DepartureTime)
//                    .ToListAsync();
//                return View("~/Views/Admin/FlightSeats.cshtml", schedules);
//            }

//            var schedule = await _context.FlightSchedules
//                .Include(x => x.Flight)
//                .Include(x => x.Bookings)
//                    .ThenInclude(t => t.Tickets)
//                        .ThenInclude(t => t.Passenger)
//                .FirstOrDefaultAsync(x => x.ScheduleId == id.Value);

//            if (schedule == null) return NotFound();

//            return View("~/Views/Admin/FlightSeats.cshtml", schedule);
//        }

//        [HttpGet("GetSeatDetails")]
//        public async Task<IActionResult> GetSeatDetails(int scheduleId, string seatNumber)
//        {
//            if (!IsAdmin()) return Unauthorized();

//            var ticket = await _context.Tickets
//                .Include(x => x.Passenger)
//                .Include(x => x.Booking)
//                    .ThenInclude(b => b.User)
//                .FirstOrDefaultAsync(x => x.Booking.ScheduleId == scheduleId && x.SeatNumber == seatNumber);

//            if (ticket == null) return NotFound();

//            return Json(new
//            {
//                success = true,
//                seatNumber = ticket.SeatNumber,
//                passengerName = ticket.Passenger?.FullName ?? "Unknown",
//                passengerType = ticket.Passenger?.PassengerType ?? "N/A",
//                status = ticket.Status,
//                bookingDate = ticket.Booking?.BookingDate?.ToString("dd/MM/yyyy HH:mm") ?? "N/A",
//                username = ticket.Booking?.User?.Username ?? "Guest"
//            });
//        }

//        [HttpPost("ToggleSeatStatus")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> ToggleSeatStatus(int scheduleId, string seatNumber)
//        {
//            if (!IsAdmin()) return Unauthorized();

//            var schedule = await _context.FlightSchedules.FirstOrDefaultAsync(x => x.ScheduleId == scheduleId);
//            if (schedule == null) return Json(new { success = false, message = "Schedule not found" });

//            // Check if there is an existing ticket
//            var existingTicket = await _context.Tickets
//                .Include(x => x.Booking)
//                .FirstOrDefaultAsync(x => x.Booking.ScheduleId == scheduleId && x.SeatNumber == seatNumber);

//            if (existingTicket != null)
//            {
//                if (existingTicket.Status == "BLOCKED")
//                {
//                    // Unblock: remove the placeholder ticket
//                    _context.Tickets.Remove(existingTicket);
                    
//                    schedule.AvailableSeats++;
//                    await _context.SaveChangesAsync();
//                    return Json(new { success = true, action = "unblocked" });
//                }
//                else
//                {
//                    return Json(new { success = false, message = "Cannot toggle status of an active reservation." });
//                }
//            }
//            else
//            {
//                // Block: create a placeholder booking and ticket
//                var systemUser = await _context.Users.FirstOrDefaultAsync(x => x.Role == "ADMIN");
//                if (systemUser == null) return Json(new { success = false, message = "No admin user found for blocking." });

//                var booking = new Booking
//                {
//                    UserId = systemUser.UserId,
//                    ScheduleId = scheduleId,
//                    BookingDate = DateTime.Now,
//                    BookingType = "ADMIN_BLOCK",
//                    Status = "BLOCKED"
//                };
//                _context.Bookings.Add(booking);
//                await _context.SaveChangesAsync();

//                var ticket = new Ticket
//                {
//                    BookingId = booking.BookingId,
//                    SeatNumber = seatNumber,
//                    Status = "BLOCKED",
//                    ClassId = (await _context.TicketClasses.FirstOrDefaultAsync())?.ClassId ?? 1
//                };
                
//                var passenger = new Passenger
//                {
//                    BookingId = booking.BookingId,
//                    FullName = "ADMIN BLOCK",
//                    PassengerType = "SYSTEM"
//                };
//                _context.Passengers.Add(passenger);
//                await _context.SaveChangesAsync();

//                ticket.PassengerId = passenger.PassengerId;
//                _context.Tickets.Add(ticket);

//                schedule.AvailableSeats--;
//                await _context.SaveChangesAsync();
//                return Json(new { success = true, action = "blocked" });
//            }
//        }
//    }
//}
