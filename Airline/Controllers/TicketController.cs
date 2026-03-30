using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Airline.Controllers
{
    public class TicketController : Controller
    {
        private const string BaggageTransactionPrefix = "BAG_";
        private readonly DataContext _context;

        public TicketController(DataContext context)
        {
            _context = context;
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
                    ticket.Booking.Status = "CANCELLED";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var message = baggageIds.Count > 0
                    ? "Ticket cancelled successfully. Any unpaid baggage registrations were removed."
                    : "Ticket cancelled successfully.";

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
