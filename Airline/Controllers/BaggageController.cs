using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Airline.Controllers
{
    public class BaggageController : Controller
    {
        private const decimal PricePerKg = 10000m;
        private readonly DataContext _context;

        public BaggageController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Redirect("/");

            var model = await BuildUserPageViewModelAsync(userId.Value);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRegister(BaggageRegisterInputModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Redirect("/");

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildUserPageViewModelAsync(userId.Value, model);
                return View("Register", invalidModel);
            }

            var ticket = await _context.Tickets
                .Include(t => t.Booking)
                .FirstOrDefaultAsync(t => t.TicketId == model.TicketId && t.Booking.UserId == userId.Value);

            if (ticket == null || !CanRegisterForTicket(ticket.Status, ticket.Booking.Status))
            {
                TempData["ErrorMessage"] = "Ban phai co ve hop le moi dang ky duoc hanh ly.";
                return RedirectToAction(nameof(Register));
            }

            var hasExistingBaggage = await _context.Baggages
                .AnyAsync(b => b.TicketId == ticket.TicketId);

            if (hasExistingBaggage)
            {
                TempData["ErrorMessage"] = "Ve nay da duoc dang ky hanh ly roi.";
                return RedirectToAction(nameof(Register));
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

            TempData["SuccessMessage"] = $"Dang ky hanh ly thanh cong cho ticket #{ticket.TicketId}.";
            return RedirectToAction(nameof(Register));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRegistration(int baggageId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Redirect("/");

            var baggage = await _context.Baggages
                .Include(b => b.Ticket)
                    .ThenInclude(t => t.Booking)
                .FirstOrDefaultAsync(b =>
                    b.BaggageId == baggageId &&
                    b.Ticket.Booking.UserId == userId.Value);

            if (baggage == null)
            {
                TempData["ErrorMessage"] = "Khong tim thay dang ky hanh ly can xoa.";
                return RedirectToAction(nameof(Register));
            }

            _context.Baggages.Remove(baggage);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Da huy dang ky hanh ly cho ticket #{baggage.TicketId}.";
            return RedirectToAction(nameof(Register));
        }

        private int? GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }

        private async Task<UserBaggagePageViewModel> BuildUserPageViewModelAsync(
            int userId,
            BaggageRegisterInputModel? form = null)
        {
            var ownedTicketRows = await _context.Tickets
                .AsNoTracking()
                .Where(t =>
                    t.Booking.UserId == userId &&
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
                    TicketStatus = t.Status,
                    HasBaggage = t.Baggages.Any()
                })
                .ToListAsync();

            var registeredRows = await _context.Baggages
                .AsNoTracking()
                .Where(b => b.Ticket.Booking.UserId == userId)
                .OrderByDescending(b => b.BaggageId)
                .Select(b => new
                {
                    b.BaggageId,
                    b.TicketId,
                    b.Ticket.BookingId,
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

            var availableTickets = ownedTicketRows
                .Where(t => !t.HasBaggage)
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

            var registeredBaggages = registeredRows
                .Select(b => new UserBaggageItemViewModel
                {
                    BaggageId = b.BaggageId,
                    TicketId = b.TicketId,
                    BookingId = b.BookingId,
                    PassengerName = b.PassengerName ?? "",
                    FlightNumber = b.FlightNumber ?? "",
                    RouteLabel = $"{b.DepartureCity} -> {b.ArrivalCity}",
                    DepartureTime = b.DepartureTime,
                    TicketStatus = b.TicketStatus ?? "",
                    Weight = b.Weight,
                    Price = b.Price
                })
                .ToList();

            return new UserBaggagePageViewModel
            {
                Form = form ?? new BaggageRegisterInputModel(),
                AvailableTickets = availableTickets,
                RegisteredBaggages = registeredBaggages,
                TotalOwnedTickets = ownedTicketRows.Count,
                EligibleTicketCount = availableTickets.Count,
                TotalRegisteredWeight = registeredBaggages.Sum(x => x.Weight),
                TotalRegisteredPrice = registeredBaggages.Sum(x => x.Price)
            };
        }

        private static bool CanRegisterForTicket(string? ticketStatus, string? bookingStatus)
        {
            return !string.Equals(ticketStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(ticketStatus, "BLOCKED", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(bookingStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(bookingStatus, "BLOCKED", StringComparison.OrdinalIgnoreCase);
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
