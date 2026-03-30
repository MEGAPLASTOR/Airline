using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminBaggageController : AdminBaseController
    {
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
        public async Task<IActionResult> CreateBaggage(BaggageRegisterInputModel model)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Du lieu hanh ly khong hop le.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            var ticket = await _context.Tickets
                .Include(t => t.Booking)
                .FirstOrDefaultAsync(t => t.TicketId == model.TicketId);

            if (ticket == null || !CanRegisterForTicket(ticket.Status, ticket.Booking.Status))
            {
                TempData["ErrorMessage"] = "Ticket khong hop le de dang ky hanh ly.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            var hasExistingBaggage = await _context.Baggages
                .AnyAsync(b => b.TicketId == ticket.TicketId);

            if (hasExistingBaggage)
            {
                TempData["ErrorMessage"] = $"Ticket #{ticket.TicketId} da co hanh ly.";
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

            TempData["SuccessMessage"] = $"Da tao hanh ly cho ticket #{ticket.TicketId}.";
            return RedirectToAction(nameof(ManageBaggage));
        }

        [HttpPost("UpdateBaggage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBaggage(BaggageUpdateWeightModel model)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Khoi luong hanh ly khong hop le.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            var baggage = await _context.Baggages
                .FirstOrDefaultAsync(b => b.BaggageId == model.BaggageId);

            if (baggage == null)
            {
                TempData["ErrorMessage"] = "Khong tim thay hanh ly can cap nhat.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            var normalizedWeight = NormalizeWeight(model.Weight!.Value);
            baggage.Weight = normalizedWeight;
            baggage.Price = CalculatePrice(normalizedWeight);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Da cap nhat hanh ly #{baggage.BaggageId}.";
            return RedirectToAction(nameof(ManageBaggage));
        }

        [HttpPost("DeleteBaggage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBaggage(int baggageId)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var baggage = await _context.Baggages
                .FirstOrDefaultAsync(b => b.BaggageId == baggageId);

            if (baggage == null)
            {
                TempData["ErrorMessage"] = "Khong tim thay hanh ly can xoa.";
                return RedirectToAction(nameof(ManageBaggage));
            }

            _context.Baggages.Remove(baggage);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Da xoa hanh ly #{baggage.BaggageId}.";
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
                .Select(b => new AdminBaggageItemViewModel
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
                    Price = b.Price
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
