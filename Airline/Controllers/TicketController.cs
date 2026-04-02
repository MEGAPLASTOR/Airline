using Airline.Models;
using Airline.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Airline.Controllers
{
    public class TicketController : Controller
    {
        private const string BaggageTransactionPrefix = "BAG_";
        private readonly BookingPaymentService _bookingPaymentService;
        private readonly DataContext _context;

        public TicketController(DataContext context, BookingPaymentService bookingPaymentService)
        {
            _context = context;
            _bookingPaymentService = bookingPaymentService;
        }

        // ==============================================================
        // GET /Ticket/ViewConfirmation
        // ==============================================================
        public async Task<IActionResult> ViewConfirmation()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);
            var reconciliationResult = await _bookingPaymentService.ReconcileSkyMilesAsync(userId);
            if (reconciliationResult.IsHandled && reconciliationResult.WasUpdated)
            {
                await RefreshAuthenticatedUserClaimsAsync(reconciliationResult);
            }

            var tickets = await _context.Tickets
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Schedule)
                        .ThenInclude(s => s.Flight)
                            .ThenInclude(f => f.Route)
                                .ThenInclude(r => r.DepartureCityNavigation)
                .Include(t => t.Booking.Schedule.Flight.Route.ArrivalCityNavigation)
                .Include(t => t.Passenger)
                .Include(t => t.Class)
                .Include(t => t.Seat)
                .Where(t => t.Booking.UserId == userId)
                .OrderByDescending(t => t.Booking.BookingDate)
                .ToListAsync();

            return View(tickets);
        }

        // ==============================================================
        // GET /Ticket/Rescheduled
        // ==============================================================
        public async Task<IActionResult> Rescheduled()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            var affectedTickets = await _context.Tickets
                .AsNoTracking()
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Schedule)
                        .ThenInclude(s => s.Flight)
                            .ThenInclude(f => f.Route)
                                .ThenInclude(r => r.DepartureCityNavigation)
                .Include(t => t.Booking.Schedule.Flight.Route.ArrivalCityNavigation)
                .Include(t => t.Passenger)
                .Include(t => t.Class)
                .Include(t => t.Seat)
                .Where(t =>
                    t.Booking.UserId == userId &&
                    t.Status != "CANCELLED" &&
                    (t.Booking.Schedule.Status == "DELAYED" ||
                     t.Booking.Schedule.Status == "CANCELLED"))
                .OrderBy(t => t.Booking.Schedule.DepartureTime)
                .ThenByDescending(t => t.Booking.BookingDate)
                .ToListAsync();

            var flights = affectedTickets
                .Select(ticket =>
                {
                    var schedule = ticket.Booking.Schedule;
                    var route = schedule.Flight.Route;
                    var normalizedScheduleStatus = (schedule.Status ?? string.Empty).ToUpperInvariant();

                    return new RescheduledFlightItemViewModel
                    {
                        TicketId = ticket.TicketId,
                        BookingId = ticket.BookingId,
                        PassengerName = ticket.Passenger?.FullName ?? "N/A",
                        FlightNumber = schedule.Flight?.FlightNumber ?? "-",
                        Origin = route?.DepartureCityNavigation?.CityName ?? "-",
                        Destination = route?.ArrivalCityNavigation?.CityName ?? "-",
                        DepartureTime = schedule.DepartureTime,
                        ArrivalTime = schedule.ArrivalTime,
                        ScheduleStatus = normalizedScheduleStatus,
                        TicketStatus = ticket.Status ?? "BOOKED",
                        SeatNumber = ticket.Seat?.SeatNumber ?? "Not assigned",
                        FareClass = ticket.Class?.ClassName ?? "-",
                        GuidanceText = normalizedScheduleStatus == "CANCELLED"
                            ? "This flight has been cancelled. Please contact support or the airline counter for rebooking assistance."
                            : "This flight has been delayed. Please use the updated departure time shown below for your trip."
                    };
                })
                .ToList();

            var viewModel = new RescheduledFlightPageViewModel
            {
                Flights = flights,
                TotalAffectedFlights = flights.Count,
                DelayedFlights = flights.Count(x => x.ScheduleStatus == "DELAYED"),
                CancelledFlights = flights.Count(x => x.ScheduleStatus == "CANCELLED")
            };

            return View("~/Views/Ticket/Rescheduled.cshtml", viewModel);
        }

        private async Task RefreshAuthenticatedUserClaimsAsync(BookingPaymentResult paymentResult)
        {
            if (User?.Identity?.IsAuthenticated != true || !paymentResult.UserId.HasValue)
            {
                return;
            }

            var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, paymentResult.UserId.Value.ToString()),
                new Claim(ClaimTypes.Name, paymentResult.Username),
                new Claim("FirstName", paymentResult.FirstName),
                new Claim("LastName", paymentResult.LastName),
                new Claim("SkyMiles", paymentResult.CurrentSkyMiles.ToString()),
                new Claim(ClaimTypes.Role, paymentResult.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authResult.Properties ?? new AuthenticationProperties());
        }

        private async Task RefreshAuthenticatedUserClaimsAsync(User user)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? string.Empty),
                new Claim("FirstName", user.FirstName ?? string.Empty),
                new Claim("LastName", user.LastName ?? string.Empty),
                new Claim("SkyMiles", (user.SkyMiles ?? 0).ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authResult.Properties ?? new AuthenticationProperties());
        }

        // ==============================================================
        // GET /Ticket/ChangeSeat/{id}
        // ==============================================================
        public async Task<IActionResult> ChangeSeat(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) 
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            var ticket = await _context.Tickets
                .Include(t => t.Passenger)
                .Include(t => t.Seat)
                .Include(t => t.Class)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Schedule)
                        .ThenInclude(s => s.Flight)
                            .ThenInclude(f => f.Route)
                .FirstOrDefaultAsync(t => t.TicketId == id && t.Booking.UserId == userId);

            if (ticket == null || ticket.Booking?.Schedule == null) return NotFound("Ticket or schedule details could not be found.");

            // Get all available seats for this schedule and class
            var availableSeats = await _context.Seats
                .Where(s => s.ScheduleId == ticket.Booking.ScheduleId 
                            && s.ClassId == ticket.ClassId 
                            && s.SeatStatus == "AVAILABLE")
                .ToListAsync();

            ViewBag.AvailableSeats = availableSeats;
            ViewBag.CurrentSeatNumber = ticket.Seat?.SeatNumber ?? "Not assigned";

            return View(ticket);
        }

        // ==============================================================
        // POST /Ticket/UpdateSeat
        // ==============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSeat(int ticketId, int newSeatId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) 
                return Json(new { success = false, message = "You do not have access to this ticket." });

            int userId = int.Parse(userIdStr);

            var ticket = await _context.Tickets
                .Include(t => t.Booking)
                .Include(t => t.Seat)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId && t.Booking.UserId == userId);

            if (ticket == null)
                return Json(new { success = false, message = "The ticket does not exist." });

            var newSeat = await _context.Seats.FindAsync(newSeatId);
            if (newSeat == null || newSeat.SeatStatus != "AVAILABLE")
                return Json(new { success = false, message = "The selected seat is no longer available." });

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Release old seat
                    if (ticket.Seat != null)
                    {
                        ticket.Seat.SeatStatus = "AVAILABLE";
                    }

                    // 2. Assign new seat
                    ticket.SeatId = newSeatId;
                    newSeat.SeatStatus = "BOOKED";

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Seat changed successfully." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Error: " + ex.Message });
                }
            }
        }

        // ==============================================================
        // POST /Ticket/CancelTicket
        // ==============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelTicket(int ticketId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
                return Json(new { success = false, message = "You need to log in to cancel this ticket." });

            int userId = int.Parse(userIdStr);

            var ticket = await _context.Tickets
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Schedule)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Tickets)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.User)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.UserPromotionsRedeemed)
                .Include(t => t.Seat)
                .Include(t => t.Baggages)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId && t.Booking.UserId == userId);

            if (ticket == null)
                return Json(new { success = false, message = "The ticket could not be found." });

            if (string.Equals(ticket.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "This ticket has already been cancelled." });

            var schedule = ticket.Booking?.Schedule;
            if (schedule == null)
                return Json(new { success = false, message = "Flight details could not be loaded for this ticket." });

            if (schedule.DepartureTime <= DateTime.Now)
            {
                return Json(new
                {
                    success = false,
                    message = "Tickets can only be cancelled before the departure time."
                });
            }

            var baggageIds = ticket.Baggages
                .Select(b => b.BaggageId)
                .ToList();

            if (baggageIds.Count > 0)
            {
                var paidBaggageTransactionNos = await _context.Payments
                    .AsNoTracking()
                    .Where(p =>
                        p.BookingId == ticket.BookingId &&
                        p.TransactionNo != null &&
                        p.TransactionNo.StartsWith(BaggageTransactionPrefix) &&
                        (p.PaymentStatus == "SUCCESS" || p.PaymentStatus == "PAID"))
                    .Select(p => p.TransactionNo!)
                    .ToListAsync();

                var hasPaidBaggage = baggageIds.Any(baggageId =>
                    paidBaggageTransactionNos.Any(txn =>
                        txn.StartsWith($"{BaggageTransactionPrefix}{baggageId}_", StringComparison.OrdinalIgnoreCase)));

                if (hasPaidBaggage)
                {
                    return Json(new
                    {
                        success = false,
                        message = "This ticket already has paid baggage. Please contact support to cancel it."
                    });
                }
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bookingWasPaid = string.Equals(ticket.Booking?.Status, "PAID", StringComparison.OrdinalIgnoreCase);
                var restoredSkyMiles = false;
                var restoredRewardCode = false;

                if (ticket.Baggages.Count > 0)
                {
                    _context.Baggages.RemoveRange(ticket.Baggages);
                }

                if (ticket.Seat != null)
                {
                    ticket.Seat.SeatStatus = "AVAILABLE";
                    ticket.SeatId = null;

                    schedule.AvailableSeats = Math.Min(
                        (schedule.AvailableSeats ?? 0) + 1,
                        schedule.TotalSeats ?? int.MaxValue);
                }

                ticket.Status = "CANCELLED";

                var hasOtherActiveTickets = ticket.Booking?.Tickets.Any(t =>
                    t.TicketId != ticket.TicketId &&
                    !string.Equals(t.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase)) == true;

                if (!hasOtherActiveTickets && ticket.Booking != null)
                {
                    if (!bookingWasPaid)
                    {
                        if (ticket.Booking.SkyMilesRedeemed > 0 && ticket.Booking.User != null)
                        {
                            ticket.Booking.User.SkyMiles = (ticket.Booking.User.SkyMiles ?? 0) + ticket.Booking.SkyMilesRedeemed;
                            ticket.Booking.SkyMilesRedeemed = 0;
                            restoredSkyMiles = true;
                        }

                        foreach (var userPromotion in ticket.Booking.UserPromotionsRedeemed)
                        {
                            userPromotion.IsRedeemed = false;
                            userPromotion.RedeemedAt = null;
                            userPromotion.RedeemedBookingId = null;
                            restoredRewardCode = true;
                        }
                    }

                    ticket.Booking.Status = "CANCELLED";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (restoredSkyMiles && ticket.Booking?.User != null)
                {
                    await RefreshAuthenticatedUserClaimsAsync(ticket.Booking.User);
                }

                var message = baggageIds.Count > 0
                    ? "Ticket cancelled successfully. Any unpaid baggage registrations were removed."
                    : "Ticket cancelled successfully.";

                if (!bookingWasPaid)
                {
                    if (restoredSkyMiles && restoredRewardCode)
                    {
                        message += " Reserved SkyMiles and the applied SkyMiles reward code are available again.";
                    }
                    else if (restoredSkyMiles)
                    {
                        message += " Reserved SkyMiles were returned to your account.";
                    }
                    else if (restoredRewardCode)
                    {
                        message += " The applied SkyMiles reward code is available again.";
                    }
                }

                return Json(new { success = true, message });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Cancellation failed: " + ex.Message });
            }
        }
    }
}
