using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminTicketStatusController : AdminBaseController
    {
        private static readonly string[] AllowedStatuses = ["BOOKED", "PAID", "CANCELLED"];

        public AdminTicketStatusController(DataContext context) : base(context)
        {
        }

        [HttpGet("TicketStatuses")]
        public async Task<IActionResult> TicketStatuses(string? search, string? status)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var normalizedSearch = search?.Trim() ?? string.Empty;
            var normalizedStatus = NormalizeStatus(status);

            var ticketQuery = _context.Tickets
                .AsNoTracking()
                .Include(t => t.Passenger)
                .Include(t => t.Class)
                .Include(t => t.Seat)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.User)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Schedule)
                        .ThenInclude(s => s.Flight)
                            .ThenInclude(f => f.Route)
                                .ThenInclude(r => r.DepartureCityNavigation)
                .Include(t => t.Booking.Schedule.Flight.Route.ArrivalCityNavigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedStatus))
            {
                ticketQuery = ticketQuery.Where(t => t.Status == normalizedStatus);
            }

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                ticketQuery = ticketQuery.Where(t =>
                    t.TicketId.ToString().Contains(normalizedSearch) ||
                    t.BookingId.ToString().Contains(normalizedSearch) ||
                    (t.Passenger.FullName != null && t.Passenger.FullName.Contains(normalizedSearch)) ||
                    (t.Booking.User.Username != null && t.Booking.User.Username.Contains(normalizedSearch)) ||
                    (t.Booking.Schedule.Flight.FlightNumber != null && t.Booking.Schedule.Flight.FlightNumber.Contains(normalizedSearch)) ||
                    (t.Seat != null && t.Seat.SeatNumber != null && t.Seat.SeatNumber.Contains(normalizedSearch)));
            }

            var tickets = await ticketQuery
                .OrderByDescending(t => t.Booking.BookingDate)
                .ThenByDescending(t => t.TicketId)
                .ToListAsync();

            var allTickets = await _context.Tickets
                .AsNoTracking()
                .ToListAsync();

            var viewModel = new AdminTicketStatusesViewModel
            {
                SearchTerm = normalizedSearch,
                SelectedStatus = normalizedStatus ?? string.Empty,
                TotalTickets = allTickets.Count,
                PaidTickets = allTickets.Count(t => string.Equals(t.Status, "PAID", StringComparison.OrdinalIgnoreCase)),
                BookedTickets = allTickets.Count(t => string.Equals(t.Status, "BOOKED", StringComparison.OrdinalIgnoreCase)),
                CancelledTickets = allTickets.Count(t => string.Equals(t.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase)),
                AvailableStatuses = AllowedStatuses,
                Tickets = tickets.Select(t => new AdminTicketStatusRowViewModel
                {
                    TicketId = t.TicketId,
                    BookingId = t.BookingId,
                    PassengerName = t.Passenger?.FullName ?? "Unknown",
                    Username = t.Booking?.User?.Username ?? "Guest",
                    FlightNumber = t.Booking?.Schedule?.Flight?.FlightNumber ?? "-",
                    RouteSummary = $"{t.Booking?.Schedule?.Flight?.Route?.DepartureCityNavigation?.CityName ?? "-"} -> {t.Booking?.Schedule?.Flight?.Route?.ArrivalCityNavigation?.CityName ?? "-"}",
                    DepartureTime = t.Booking?.Schedule?.DepartureTime,
                    BookingDate = t.Booking?.BookingDate,
                    SeatNumber = t.Seat?.SeatNumber ?? "Not assigned",
                    FareClass = t.Class?.ClassName ?? "-",
                    Status = t.Status,
                    IsLocked = string.Equals(t.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase)
                }).ToList()
            };

            return View("~/Views/Admin/TicketStatuses.cshtml", viewModel);
        }

        [HttpPost("TicketStatuses/Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int ticketId, string status, string? search, string? filterStatus)
        {
            if (!IsAdmin()) return Unauthorized();

            var normalizedStatus = NormalizeStatus(status);
            if (normalizedStatus == null)
            {
                TempData["ErrorMessage"] = "Invalid ticket status.";
                return RedirectToTicketStatuses(search, filterStatus);
            }

            var ticket = await _context.Tickets
                .Include(t => t.Seat)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Tickets)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Schedule)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Ticket not found.";
                return RedirectToTicketStatuses(search, filterStatus);
            }

            if (!CanTransition(ticket.Status, normalizedStatus))
            {
                TempData["ErrorMessage"] = $"Ticket #{ticketId} cannot move from {ticket.Status} to {normalizedStatus}.";
                return RedirectToTicketStatuses(search, filterStatus);
            }

            if (string.Equals(ticket.Status, normalizedStatus, StringComparison.OrdinalIgnoreCase))
            {
                TempData["SuccessMessage"] = $"Ticket #{ticketId} is already {normalizedStatus}.";
                return RedirectToTicketStatuses(search, filterStatus);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var isCancelling = string.Equals(normalizedStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase);
                if (isCancelling && ticket.Seat != null)
                {
                    ticket.Seat.SeatStatus = "AVAILABLE";
                    ticket.SeatId = null;

                    var schedule = ticket.Booking?.Schedule;
                    if (schedule != null)
                    {
                        schedule.AvailableSeats = Math.Min(
                            (schedule.AvailableSeats ?? 0) + 1,
                            schedule.TotalSeats ?? int.MaxValue);
                    }
                }

                ticket.Status = normalizedStatus;

                if (ticket.Booking != null)
                {
                    ticket.Booking.Status = ResolveBookingStatus(ticket.Booking.Tickets);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Updated Ticket #{ticketId} to {normalizedStatus}.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Failed to update Ticket #{ticketId}: {ex.Message}";
            }

            return RedirectToTicketStatuses(search, filterStatus);
        }

        private IActionResult RedirectToTicketStatuses(string? search, string? filterStatus)
        {
            return RedirectToAction(nameof(TicketStatuses), new { search, status = filterStatus });
        }

        private static string? NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            var normalizedStatus = status.Trim().ToUpperInvariant();
            return AllowedStatuses.Contains(normalizedStatus) ? normalizedStatus : null;
        }

        private static bool CanTransition(string currentStatus, string nextStatus)
        {
            if (string.Equals(currentStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(nextStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(currentStatus, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(nextStatus, "PAID", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(nextStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase);
            }

            return AllowedStatuses.Contains(nextStatus);
        }

        private static string ResolveBookingStatus(IEnumerable<Ticket> tickets)
        {
            var activeTickets = tickets
                .Where(t => !string.Equals(t.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!activeTickets.Any())
            {
                return "CANCELLED";
            }

            if (activeTickets.All(t => string.Equals(t.Status, "PAID", StringComparison.OrdinalIgnoreCase)))
            {
                return "PAID";
            }

            return "PENDING_PAYMENT";
        }
    }
}
