using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Airline.Models;
using System.Security.Claims;
using Airline.Services;

namespace Airline.Controllers
{
    public class BookingController : Controller
    {
        private readonly DataContext _context;
        private readonly SeatService _seatService;

        public BookingController(DataContext context, SeatService seatService)
        {
            _context = context;
            _seatService = seatService;
        }

        // 1. Search and Select Flight
        public async Task<IActionResult> BookFlight(string origin, string destination)
        {
            var query = _context.FlightSchedules
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                .Include(s => s.Flight.Route.ArrivalCityNavigation)
                .Include(s => s.TicketPrices)
                .Where(s => s.DepartureTime > DateTime.Now && s.Status == "SCHEDULED" && s.TicketPrices.Any())
                .OrderBy(s => s.DepartureTime)
                .AsQueryable();

            if (!string.IsNullOrEmpty(origin))
            {
                query = query.Where(s => s.Flight.Route.DepartureCityNavigation.CityName.Contains(origin));
            }

            if (!string.IsNullOrEmpty(destination))
            {
                query = query.Where(s => s.Flight.Route.ArrivalCityNavigation.CityName.Contains(destination));
            }

            var schedules = await query.ToListAsync();
            ViewBag.Origin = origin;
            ViewBag.Destination = destination;

            return View(schedules);
        }

        // 2. Select Seat
        public async Task<IActionResult> SelectSeat(int id)
        {
            // Ensure seats exist for this schedule (4-cabin generation)
            await _seatService.GenerateSeatsAsync(id);

            var schedule = await _context.FlightSchedules
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                .Include(s => s.Flight.Route.ArrivalCityNavigation)
                .Include(s => s.Seats)
                    .ThenInclude(seat => seat.Class)
                .Include(s => s.TicketPrices)
                    .ThenInclude(p => p.Class)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null) return NotFound();

            return View(schedule);
        }

        // 3. Passenger Information
        [HttpPost]
        public async Task<IActionResult> PassengerInfo(int scheduleId, int seatId)
        {
            var schedule = await _context.FlightSchedules
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                .Include(s => s.Flight.Route.ArrivalCityNavigation)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

            var seat = await _context.Seats
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.SeatId == seatId);

            if (schedule == null || seat == null) return NotFound();

            // Fetch price for this cabin class
            var priceEntry = await _context.TicketPrices
                .FirstOrDefaultAsync(p => p.ScheduleId == scheduleId && p.ClassId == seat.ClassId);

            var model = new BookingViewModel
            {
                ScheduleId = scheduleId,
                SeatId = seatId,
                ClassId = seat.ClassId,
                SeatNumber = seat.SeatNumber,
                FlightNumber = schedule.Flight?.FlightNumber ?? "Unknown",
                DepartureTime = schedule.DepartureTime,
                Origin = schedule.Flight?.Route?.DepartureCityNavigation?.CityName ?? "Unknown",
                Destination = schedule.Flight?.Route?.ArrivalCityNavigation?.CityName ?? "Unknown",
                Price = priceEntry?.Price ?? 1500000 // Fallback price
            };

            return View(model);
        }

        // GET: ConfirmBooking (Redirect back if hit directly or refreshed)
        [HttpGet]
        public IActionResult ConfirmBooking()
        {
            return RedirectToAction("BookFlight");
        }

        // POST: Confirm and Save Booking
        [HttpPost]
        [IgnoreAntiforgeryToken] // Rule out Antiforgery issues for now
        public async Task<IActionResult> ConfirmBooking([FromForm] BookingViewModel model)
        {
            if (model == null) return BadRequest("Model is null");

            if (!ModelState.IsValid)
            {
                return View("PassengerInfo", model);
            }

            // Authentication check - Redirect to "/" if not logged in
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Redirect("/"); 
            int userId = int.Parse(userIdStr);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Refresh data from DB to ensure integrity and avoid hidden field tampering/formatting issues
                    var schedule = await _context.FlightSchedules
                        .Include(s => s.Flight)
                            .ThenInclude(f => f.Route)
                                .ThenInclude(r => r.DepartureCityNavigation)
                        .Include(s => s.Flight.Route.ArrivalCityNavigation)
                        .FirstOrDefaultAsync(s => s.ScheduleId == model.ScheduleId);

                    var seat = await _context.Seats.FindAsync(model.SeatId);

                    if (schedule == null || seat == null || seat.SeatStatus != "AVAILABLE")
                    {
                         throw new Exception("Chỗ ngồi không còn sẵn hoặc lịch trình không tồn tại.");
                    }

                    // 1. Create Booking
                    var booking = new Booking
                    {
                        UserId = userId,
                        ScheduleId = model.ScheduleId,
                        BookingDate = DateTime.Now,
                        BookingType = "ONLINE",
                        Status = "PENDING_PAYMENT"
                    };
                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();

                    // 2. Create Passenger
                    var passenger = new Passenger
                    {
                        BookingId = booking.BookingId,
                        FullName = model.FullName ?? "Người mới",
                        PassengerType = model.PassengerType ?? "Người lớn"
                    };
                    _context.Passengers.Add(passenger);
                    await _context.SaveChangesAsync();

                    // 3. Create Ticket
                    var ticket = new Ticket
                    {
                        BookingId = booking.BookingId,
                        PassengerId = passenger.PassengerId,
                        ClassId = seat.ClassId,
                        SeatId = seat.SeatId,
                        Status = "BOOKED"
                    };
                    _context.Tickets.Add(ticket);

                    // 4. Update Seat & Schedule
                    seat.SeatStatus = "BOOKED";
                    schedule.AvailableSeats = (schedule.AvailableSeats ?? 0) - 1;
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return View("BookingSuccess", booking.BookingId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Đặt vé thất bại: " + ex.Message);
                    return View("PassengerInfo", model);
                }
            }
        }
    }
}
