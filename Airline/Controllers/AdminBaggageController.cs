using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminBaggageController : AdminBaseController
    {
        private const string BaggageTransactionPrefix = "BAG_";
        private const decimal PricePerKg = 10000m;

        public AdminBaggageController(DataContext context) : base(context)
        {
        }

        [HttpGet("ManageBaggage")]
        public async Task<IActionResult> ManageBaggage()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var model = await BuildAdminPageViewModelAsync();
            return View("~/Views/Admin/AdminBaggage.cshtml", model);
        }

        [HttpPost("CreateBaggage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBaggage([Bind(Prefix = "CreateForm")] BaggageRegisterInputModel model)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid baggage data.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            var ticket = await _context.Tickets
                .Include(t => t.Booking)
                .FirstOrDefaultAsync(t => t.TicketId == model.TicketId);

            if (ticket == null || !CanRegisterForTicket(ticket.Status, ticket.Booking.Status))
            {
                TempData["ErrorMessage"] = "This ticket is not eligible for baggage registration.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            var hasExistingBaggage = await _context.Baggages
                .AnyAsync(b => b.TicketId == ticket.TicketId);

            if (hasExistingBaggage)
            {
                TempData["ErrorMessage"] = $"Ticket #{ticket.TicketId} already has registered baggage.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            var normalizedWeight = NormalizeWeight(model.Weight!.Value);
            var baggage = new Baggage
            {
                TicketId = ticket.TicketId,
                Weight = normalizedWeight,
                Price = CalculatePrice(normalizedWeight)
            };

            _context.Baggages.Add(baggage);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Created baggage for ticket #{ticket.TicketId}.";
            return RedirectToAction(nameof(ManageBaggage));
        }

        [HttpPost("UpdateBaggage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBaggage(BaggageUpdateWeightModel model)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid baggage weight.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            var baggage = await _context.Baggages
                .Include(b => b.Ticket)
                .FirstOrDefaultAsync(b => b.BaggageId == model.BaggageId);

            if (baggage == null)
            {
                TempData["ErrorMessage"] = "Baggage record to update was not found.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            if (await IsBaggagePaidAsync(baggage.BaggageId, baggage.Ticket.BookingId))
            {
                TempData["ErrorMessage"] = "Paid baggage cannot be edited.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            var normalizedWeight = NormalizeWeight(model.Weight!.Value);
            baggage.Weight = normalizedWeight;
            baggage.Price = CalculatePrice(normalizedWeight);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Updated baggage #{baggage.BaggageId}.";
            return RedirectToAction(nameof(ManageBaggage));
        }

        [HttpPost("ConfirmBaggagePayment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBaggagePayment(int baggageId)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var baggage = await _context.Baggages
                .Include(b => b.Ticket)
                .FirstOrDefaultAsync(b => b.BaggageId == baggageId);

            if (baggage == null)
            {
                TempData["ErrorMessage"] = "Baggage record was not found.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            if (await IsBaggagePaidAsync(baggage.BaggageId, baggage.Ticket.BookingId))
            {
                TempData["SuccessMessage"] = $"Baggage #{baggage.BaggageId} has already been confirmed as paid.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            var amount = baggage.Price ?? 0m;
            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "The baggage fee is invalid for confirmation.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Payments.Add(new Payment
                {
                    BookingId = baggage.Ticket.BookingId,
                    Amount = amount,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = "MANUAL_ADMIN_BAGGAGE",
                    PaymentStatus = "SUCCESS",
                    TransactionNo = $"{BaggageTransactionPrefix}{baggage.BaggageId}_MANUAL_{DateTime.Now.Ticks}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Confirmed baggage payment for baggage #{baggage.BaggageId}. The user now sees this baggage as PAID.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Failed to confirm baggage payment: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageBaggage));
        }

        [HttpPost("DeleteBaggage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBaggage(int baggageId)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var baggage = await _context.Baggages
                .Include(b => b.Ticket)
                .FirstOrDefaultAsync(b => b.BaggageId == baggageId);

            if (baggage == null)
            {
                TempData["ErrorMessage"] = "Baggage record to delete was not found.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            var isPaid = await _context.Payments
                .AsNoTracking()
                .AnyAsync(p =>
                    p.BookingId == baggage.Ticket.BookingId &&
                    p.TransactionNo != null &&
                    p.TransactionNo.StartsWith($"{BaggageTransactionPrefix}{baggageId}_") &&
                    (p.PaymentStatus == "SUCCESS" || p.PaymentStatus == "PAID"));

            if (isPaid)
            {
                TempData["ErrorMessage"] = "Paid baggage cannot be deleted.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            _context.Baggages.Remove(baggage);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Deleted baggage #{baggage.BaggageId}.";
            return RedirectToAction(nameof(ManageBaggage));
        }

        private async Task<AdminBaggagePageViewModel> BuildAdminPageViewModelAsync()
        {
            var baggageRows = await _context.Baggages
                .AsNoTracking()
                .OrderByDescending(b => b.BaggageId)
                .Select(b => new
                {
                    b.BaggageId,
                    b.TicketId,
                    b.Ticket.BookingId,
                    UserId = b.Ticket.Booking.UserId,
                    UserFirstName = b.Ticket.Booking.User.FirstName,
                    UserLastName = b.Ticket.Booking.User.LastName,
                    UserName = b.Ticket.Booking.User.Username,
                    PassengerName = b.Ticket.Passenger.FullName,
                    FlightNumber = b.Ticket.Booking.Schedule.Flight.FlightNumber,
                    DepartureCity = b.Ticket.Booking.Schedule.Flight.Route.DepartureCityNavigation.CityName,
                    ArrivalCity = b.Ticket.Booking.Schedule.Flight.Route.ArrivalCityNavigation.CityName,
                    b.Ticket.Booking.Schedule.DepartureTime,
                    TicketStatus = b.Ticket.Status,
                    Weight = b.Weight ?? 0m,
                    Price = b.Price ?? 0m
                })
                .ToListAsync();

            var bookingIds = baggageRows
                .Select(x => x.BookingId)
                .Distinct()
                .ToList();

            var baggagePayments = bookingIds.Count == 0
                ? []
                : await _context.Payments
                    .AsNoTracking()
                    .Where(p =>
                        bookingIds.Contains(p.BookingId) &&
                        p.TransactionNo != null &&
                        p.TransactionNo.StartsWith(BaggageTransactionPrefix))
                    .Select(p => new
                    {
                        p.BookingId,
                        p.PaymentStatus,
                        p.PaymentDate,
                        p.TransactionNo
                    })
                    .ToListAsync();

            var availableTicketRows = await _context.Tickets
                .AsNoTracking()
                .Where(t =>
                    !t.Baggages.Any() &&
                    t.Status != "CANCELLED" &&
                    t.Status != "BLOCKED" &&
                    t.Booking.Status != "CANCELLED" &&
                    t.Booking.Status != "BLOCKED")
                .OrderByDescending(t => t.Booking.BookingDate)
                .ThenByDescending(t => t.TicketId)
                .Select(t => new
                {
                    t.TicketId,
                    t.BookingId,
                    t.Booking.UserId,
                    UserFirstName = t.Booking.User.FirstName,
                    UserLastName = t.Booking.User.LastName,
                    UserName = t.Booking.User.Username,
                    PassengerName = t.Passenger.FullName,
                    FlightNumber = t.Booking.Schedule.Flight.FlightNumber,
                    DepartureCity = t.Booking.Schedule.Flight.Route.DepartureCityNavigation.CityName,
                    ArrivalCity = t.Booking.Schedule.Flight.Route.ArrivalCityNavigation.CityName,
                    t.Booking.Schedule.DepartureTime,
                    TicketStatus = t.Status
                })
                .ToListAsync();

            var baggageItems = baggageRows
                .Select(b =>
                {
                    var payment = baggagePayments
                        .Where(p =>
                            p.BookingId == b.BookingId &&
                            p.TransactionNo != null &&
                            p.TransactionNo.StartsWith($"{BaggageTransactionPrefix}{b.BaggageId}_", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(p => p.PaymentDate)
                        .FirstOrDefault();

                    var paymentStatus = payment?.PaymentStatus ?? "UNPAID";
                    var isPaid = string.Equals(paymentStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(paymentStatus, "PAID", StringComparison.OrdinalIgnoreCase);

                    return new AdminBaggageItemViewModel
                    {
                        BaggageId = b.BaggageId,
                        TicketId = b.TicketId,
                        BookingId = b.BookingId,
                        UserId = b.UserId,
                        CustomerName = $"{b.UserFirstName} {b.UserLastName}".Trim(),
                        Username = b.UserName ?? "",
                        PassengerName = b.PassengerName ?? "",
                        FlightNumber = b.FlightNumber ?? "",
                        RouteLabel = $"{b.DepartureCity} -> {b.ArrivalCity}",
                        DepartureTime = b.DepartureTime,
                        TicketStatus = b.TicketStatus ?? "",
                        Weight = b.Weight,
                        Price = b.Price,
                        IsPaid = isPaid,
                        PaymentStatus = isPaid ? "PAID" : "UNPAID"
                    };
                })
                .ToList();

            var availableTickets = availableTicketRows
                .Select(t => new BaggageTicketOptionViewModel
                {
                    TicketId = t.TicketId,
                    BookingId = t.BookingId,
                    UserId = t.UserId,
                    CustomerName = $"{t.UserFirstName} {t.UserLastName}".Trim(),
                    Username = t.UserName ?? "",
                    PassengerName = t.PassengerName ?? "",
                    FlightNumber = t.FlightNumber ?? "",
                    RouteLabel = $"{t.DepartureCity} -> {t.ArrivalCity}",
                    DepartureTime = t.DepartureTime,
                    TicketStatus = t.TicketStatus ?? ""
                })
                .ToList();

            return new AdminBaggagePageViewModel
            {
                AvailableTickets = availableTickets,
                Baggages = baggageItems,
                TotalBaggages = baggageItems.Count,
                TotalCustomers = baggageItems.Select(x => x.UserId).Distinct().Count(),
                TotalWeight = baggageItems.Sum(x => x.Weight),
                TotalRevenue = baggageItems.Sum(x => x.Price)
            };
        }

        private static bool CanRegisterForTicket(string? ticketStatus, string? bookingStatus)
        {
            return !string.Equals(ticketStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(ticketStatus, "BLOCKED", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(bookingStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(bookingStatus, "BLOCKED", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> IsBaggagePaidAsync(int baggageId, int bookingId)
        {
            return await _context.Payments
                .AsNoTracking()
                .AnyAsync(p =>
                    p.BookingId == bookingId &&
                    p.TransactionNo != null &&
                    p.TransactionNo.StartsWith($"{BaggageTransactionPrefix}{baggageId}_") &&
                    (p.PaymentStatus == "SUCCESS" || p.PaymentStatus == "PAID"));
        }

        private static decimal NormalizeWeight(decimal weight)
        {
            return Math.Round(weight, 2, MidpointRounding.AwayFromZero);
        }

        private static decimal CalculatePrice(decimal weight)
        {
            return Math.Round(weight * PricePerKg, 2, MidpointRounding.AwayFromZero);
        }
    }
}
