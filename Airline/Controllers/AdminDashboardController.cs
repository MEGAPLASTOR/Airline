using System.Globalization;
using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminDashboardController : AdminBaseController
    {
        private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

        public AdminDashboardController(DataContext context) : base(context)
        {
        }

        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var model = await BuildDashboardViewModelAsync();
            return View("~/Views/Admin/AdminDashboard.cshtml", model);
        }

        [HttpGet("AdminDashboard")]
        public async Task<IActionResult> AdminDashboard()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var model = await BuildDashboardViewModelAsync();
            return View("~/Views/Admin/AdminDashboard.cshtml", model);
        }

        private async Task<AdminDashboardViewModel> BuildDashboardViewModelAsync()
        {
            var now = DateTime.Now;
            var today = now.Date;
            var tomorrow = today.AddDays(1);
            var yesterday = today.AddDays(-1);

            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = currentMonthStart.AddMonths(1);
            var previousMonthStart = currentMonthStart.AddMonths(-1);
            var yearStart = new DateTime(now.Year, 1, 1);
            var nextYearStart = yearStart.AddYears(1);
            var last7DaysStart = today.AddDays(-6);

            var totalUsers = await _context.Users.AsNoTracking().CountAsync();
            var usersThisMonth = await _context.Users.AsNoTracking()
                .CountAsync(u => u.CreatedAt.HasValue && u.CreatedAt >= currentMonthStart && u.CreatedAt < nextMonthStart);
            var usersPreviousMonth = await _context.Users.AsNoTracking()
                .CountAsync(u => u.CreatedAt.HasValue && u.CreatedAt >= previousMonthStart && u.CreatedAt < currentMonthStart);

            var revenueThisMonth = await _context.Payments.AsNoTracking()
                .Where(p => p.PaymentDate.HasValue &&
                            p.PaymentDate >= currentMonthStart &&
                            p.PaymentDate < nextMonthStart &&
                            (p.PaymentStatus == "PAID" || p.PaymentStatus == "SUCCESS"))
                .SumAsync(p => p.Amount ?? 0m);

            var revenuePreviousMonth = await _context.Payments.AsNoTracking()
                .Where(p => p.PaymentDate.HasValue &&
                            p.PaymentDate >= previousMonthStart &&
                            p.PaymentDate < currentMonthStart &&
                            (p.PaymentStatus == "PAID" || p.PaymentStatus == "SUCCESS"))
                .SumAsync(p => p.Amount ?? 0m);

            var activeBookings = await _context.Bookings.AsNoTracking()
                .CountAsync(b =>
                    b.BookingType != "ADMIN_BLOCK" &&
                    b.Status != "CANCELLED" &&
                    b.Status != "BLOCKED");

            var bookingsThisMonth = await _context.Bookings.AsNoTracking()
                .CountAsync(b =>
                    b.BookingDate.HasValue &&
                    b.BookingDate >= currentMonthStart &&
                    b.BookingDate < nextMonthStart &&
                    b.BookingType != "ADMIN_BLOCK");

            var bookingsPreviousMonth = await _context.Bookings.AsNoTracking()
                .CountAsync(b =>
                    b.BookingDate.HasValue &&
                    b.BookingDate >= previousMonthStart &&
                    b.BookingDate < currentMonthStart &&
                    b.BookingType != "ADMIN_BLOCK");

            var flightsToday = await _context.FlightSchedules.AsNoTracking()
                .CountAsync(s => s.DepartureTime >= today && s.DepartureTime < tomorrow);

            var flightsYesterday = await _context.FlightSchedules.AsNoTracking()
                .CountAsync(s => s.DepartureTime >= yesterday && s.DepartureTime < today);

            var userSparkline = await BuildDailyCountSeriesAsync(
                last7DaysStart,
                day => _context.Users.AsNoTracking().CountAsync(u =>
                    u.CreatedAt.HasValue &&
                    u.CreatedAt >= day &&
                    u.CreatedAt < day.AddDays(1)));

            var revenueSparkline = await BuildDailyDecimalSeriesAsync(
                last7DaysStart,
                day => _context.Payments.AsNoTracking()
                    .Where(p =>
                        p.PaymentDate.HasValue &&
                        p.PaymentDate >= day &&
                        p.PaymentDate < day.AddDays(1) &&
                        (p.PaymentStatus == "PAID" || p.PaymentStatus == "SUCCESS"))
                    .SumAsync(p => p.Amount ?? 0m));

            var bookingSparkline = await BuildDailyCountSeriesAsync(
                last7DaysStart,
                day => _context.Bookings.AsNoTracking().CountAsync(b =>
                    b.BookingDate.HasValue &&
                    b.BookingDate >= day &&
                    b.BookingDate < day.AddDays(1) &&
                    b.BookingType != "ADMIN_BLOCK"));

            var flightSparkline = await BuildDailyCountSeriesAsync(
                last7DaysStart,
                day => _context.FlightSchedules.AsNoTracking().CountAsync(s =>
                    s.DepartureTime >= day &&
                    s.DepartureTime < day.AddDays(1)));

            var bookingChartLabels = Enumerable.Range(1, 12)
                .Select(month => CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames[month - 1].TrimEnd('.'))
                .ToArray();

            var monthlyBookingCounts = await _context.Bookings.AsNoTracking()
                .Where(b =>
                    b.BookingDate.HasValue &&
                    b.BookingDate >= yearStart &&
                    b.BookingDate < nextYearStart &&
                    b.BookingType != "ADMIN_BLOCK")
                .GroupBy(b => b.BookingDate!.Value.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            var bookingChartValues = Enumerable.Range(1, 12)
                .Select(month => monthlyBookingCounts.FirstOrDefault(x => x.Month == month)?.Count ?? 0)
                .ToArray();

            var monthlyRevenue = await _context.Payments.AsNoTracking()
                .Where(p =>
                    p.PaymentDate.HasValue &&
                    p.PaymentDate >= yearStart &&
                    p.PaymentDate < nextYearStart &&
                    (p.PaymentStatus == "PAID" || p.PaymentStatus == "SUCCESS"))
                .GroupBy(p => p.PaymentDate!.Value.Month)
                .Select(g => new { Month = g.Key, Revenue = g.Sum(x => x.Amount ?? 0m) })
                .ToListAsync();

            var revenueChartValues = Enumerable.Range(1, 12)
                .Select(month => monthlyRevenue.FirstOrDefault(x => x.Month == month)?.Revenue ?? 0m)
                .ToArray();

            var classDistributionRaw = await _context.Tickets.AsNoTracking()
                .Where(t =>
                    t.Booking.BookingType != "ADMIN_BLOCK" &&
                    t.Status != "BLOCKED")
                .GroupBy(t => t.Class.ClassName)
                .Select(g => new
                {
                    ClassName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var totalDistributedTickets = classDistributionRaw.Sum(x => x.Count);
            var ticketClasses = classDistributionRaw
                .Select(x => new AdminDashboardDistributionViewModel
                {
                    Label = x.ClassName,
                    Count = x.Count,
                    Percentage = totalDistributedTickets == 0
                        ? 0
                        : Math.Round((decimal)x.Count * 100m / totalDistributedTickets, 1)
                })
                .ToArray();

            var topRoutesRaw = await _context.Tickets.AsNoTracking()
                .Where(t =>
                    t.Booking.BookingType != "ADMIN_BLOCK" &&
                    t.Status != "BLOCKED")
                .GroupBy(t => new
                {
                    Departure = t.Booking.Schedule.Flight.Route.DepartureCityNavigation.CityName,
                    Arrival = t.Booking.Schedule.Flight.Route.ArrivalCityNavigation.CityName
                })
                .Select(g => new
                {
                    g.Key.Departure,
                    g.Key.Arrival,
                    PassengerCount = g.Count()
                })
                .OrderByDescending(x => x.PassengerCount)
                .Take(5)
                .ToListAsync();

            var topRoutes = topRoutesRaw
                .Select(x => new AdminDashboardRouteViewModel
                {
                    Label = $"{x.Departure} -> {x.Arrival}",
                    PassengerCount = x.PassengerCount
                })
                .ToArray();

            var recentTicketRows = await _context.Tickets.AsNoTracking()
                .Where(t => t.Booking.BookingType != "ADMIN_BLOCK")
                .OrderByDescending(t => t.Booking.BookingDate)
                .ThenByDescending(t => t.TicketId)
                .Select(t => new
                {
                    t.TicketId,
                    PassengerName = t.Passenger.FullName,
                    Departure = t.Booking.Schedule.Flight.Route.DepartureCityNavigation.CityName,
                    Arrival = t.Booking.Schedule.Flight.Route.ArrivalCityNavigation.CityName,
                    ClassName = t.Class.ClassName,
                    t.Status
                })
                .Take(6)
                .ToListAsync();

            var recentTickets = recentTicketRows
                .Select(t =>
                {
                    var (statusLabel, statusCssClass) = MapTicketStatus(t.Status);
                    return new AdminDashboardRecentTicketViewModel
                    {
                        TicketCode = $"#SKY-{t.TicketId:D5}",
                        PassengerName = t.PassengerName,
                        RouteLabel = $"{t.Departure} -> {t.Arrival}",
                        ClassName = t.ClassName,
                        StatusLabel = statusLabel,
                        StatusCssClass = statusCssClass
                    };
                })
                .ToArray();

            var totalSeats = await _context.FlightSchedules.AsNoTracking()
                .SumAsync(s => s.TotalSeats ?? 0);
            var availableSeats = await _context.FlightSchedules.AsNoTracking()
                .SumAsync(s => s.AvailableSeats ?? 0);
            var bookedSeats = Math.Max(0, totalSeats - availableSeats);

            var totalPayments = await _context.Payments.AsNoTracking().CountAsync();
            var successfulPayments = await _context.Payments.AsNoTracking()
                .CountAsync(p => p.PaymentStatus == "PAID" || p.PaymentStatus == "SUCCESS");

            var totalSchedules = await _context.FlightSchedules.AsNoTracking().CountAsync();
            var onTimeSchedules = await _context.FlightSchedules.AsNoTracking()
                .CountAsync(s => s.Status == "SCHEDULED" || s.Status == "COMPLETED");

            var totalCustomerBookings = await _context.Bookings.AsNoTracking()
                .CountAsync(b => b.BookingType != "ADMIN_BLOCK");
            var pendingConfirmations = await _context.Bookings.AsNoTracking()
                .CountAsync(b => b.BookingType != "ADMIN_BLOCK" && b.Status == "CONFIRMED");

            var operationalMetrics = new[]
            {
                new AdminDashboardHealthViewModel
                {
                    Name = "Seat Occupancy",
                    Percentage = ToPercent(bookedSeats, totalSeats),
                    ValueText = $"{ToPercent(bookedSeats, totalSeats)}%",
                    CssClass = GetMetricCssClass(ToPercent(bookedSeats, totalSeats), reverse: false)
                },
                new AdminDashboardHealthViewModel
                {
                    Name = "Payment Success",
                    Percentage = ToPercent(successfulPayments, totalPayments),
                    ValueText = $"{ToPercent(successfulPayments, totalPayments)}%",
                    CssClass = GetMetricCssClass(ToPercent(successfulPayments, totalPayments), reverse: false)
                },
                new AdminDashboardHealthViewModel
                {
                    Name = "On-time Schedules",
                    Percentage = ToPercent(onTimeSchedules, totalSchedules),
                    ValueText = $"{ToPercent(onTimeSchedules, totalSchedules)}%",
                    CssClass = GetMetricCssClass(ToPercent(onTimeSchedules, totalSchedules), reverse: false)
                },
                new AdminDashboardHealthViewModel
                {
                    Name = "Pending Confirmations",
                    Percentage = ToPercent(pendingConfirmations, totalCustomerBookings),
                    ValueText = $"{pendingConfirmations} booking(s)",
                    CssClass = GetMetricCssClass(ToPercent(pendingConfirmations, totalCustomerBookings), reverse: true)
                }
            };

            return new AdminDashboardViewModel
            {
                AdminFirstName = User?.FindFirst("FirstName")?.Value ?? "Admin",
                CurrentYear = now.Year,
                TotalUsers = BuildKpi(
                    FormatCompactNumber(totalUsers),
                    "Total Users",
                    usersThisMonth,
                    usersPreviousMonth,
                    userSparkline),
                RevenueThisMonth = BuildKpi(
                    FormatCurrency(revenueThisMonth),
                    "Revenue (This Month)",
                    revenueThisMonth,
                    revenuePreviousMonth,
                    revenueSparkline),
                ActiveBookings = BuildKpi(
                    FormatCompactNumber(activeBookings),
                    "Active Bookings",
                    bookingsThisMonth,
                    bookingsPreviousMonth,
                    bookingSparkline),
                FlightsToday = BuildKpi(
                    FormatCompactNumber(flightsToday),
                    "Flights Today",
                    flightsToday,
                    flightsYesterday,
                    flightSparkline),
                BookingChartLabels = bookingChartLabels,
                BookingChartValues = bookingChartValues,
                RevenueChartLabels = bookingChartLabels,
                RevenueChartValues = revenueChartValues,
                TicketClasses = ticketClasses,
                TopRoutes = topRoutes,
                RecentTickets = recentTickets,
                OperationalMetrics = operationalMetrics
            };
        }

        private static AdminDashboardKpiViewModel BuildKpi(
            string value,
            string label,
            decimal currentPeriodValue,
            decimal previousPeriodValue,
            decimal[] sparkline)
        {
            var trend = BuildTrend(currentPeriodValue, previousPeriodValue);

            return new AdminDashboardKpiViewModel
            {
                Value = value,
                Label = label,
                TrendText = trend.Text,
                TrendDirection = trend.Direction,
                Sparkline = sparkline
            };
        }

        private static (string Text, string Direction) BuildTrend(decimal currentValue, decimal previousValue)
        {
            if (previousValue == 0)
            {
                if (currentValue == 0)
                {
                    return ("0.0%", "flat");
                }

                return ("New", "up");
            }

            var percentage = Math.Round((currentValue - previousValue) * 100m / previousValue, 1);
            if (percentage > 0) return ($"+{percentage:0.0}%", "up");
            if (percentage < 0) return ($"{percentage:0.0}%", "down");
            return ("0.0%", "flat");
        }

        private static async Task<decimal[]> BuildDailyCountSeriesAsync(DateTime startDate, Func<DateTime, Task<int>> loader)
        {
            var values = new decimal[7];

            for (var i = 0; i < values.Length; i++)
            {
                var day = startDate.AddDays(i);
                values[i] = await loader(day);
            }

            return values;
        }

        private static async Task<decimal[]> BuildDailyDecimalSeriesAsync(DateTime startDate, Func<DateTime, Task<decimal>> loader)
        {
            var values = new decimal[7];

            for (var i = 0; i < values.Length; i++)
            {
                var day = startDate.AddDays(i);
                values[i] = await loader(day);
            }

            return values;
        }

        private static string FormatCompactNumber(decimal value)
        {
            if (value >= 1_000_000_000m) return $"{value / 1_000_000_000m:0.##}B";
            if (value >= 1_000_000m) return $"{value / 1_000_000m:0.##}M";
            if (value >= 1_000m) return $"{value / 1_000m:0.##}K";

            return value.ToString("#,0", ViCulture);
        }

        private static string FormatCurrency(decimal amount)
        {
            if (amount >= 1_000_000_000m) return $"{amount / 1_000_000_000m:0.##}B VND";
            if (amount >= 1_000_000m) return $"{amount / 1_000_000m:0.##}M VND";
            if (amount >= 1_000m) return $"{amount / 1_000m:0.##}K VND";

            return $"{amount.ToString("#,0", ViCulture)} VND";
        }

        private static int ToPercent(decimal numerator, decimal denominator)
        {
            if (denominator <= 0) return 0;
            return (int)Math.Round(numerator * 100m / denominator, MidpointRounding.AwayFromZero);
        }

        private static string GetMetricCssClass(int percentage, bool reverse)
        {
            if (reverse)
            {
                if (percentage >= 40) return "crit";
                if (percentage >= 15) return "warn";
                return string.Empty;
            }

            if (percentage < 50) return "crit";
            if (percentage < 75) return "warn";
            return string.Empty;
        }

        private static (string Label, string CssClass) MapTicketStatus(string? status)
        {
            return status?.ToUpperInvariant() switch
            {
                "PAID" => ("Paid", "pill-green"),
                "BOOKED" => ("Booked", "pill-green"),
                "ACTIVE" => ("Active", "pill-green"),
                "CONFIRMED" => ("Pending", "pill-gold"),
                "CANCELLED" => ("Cancelled", "pill-red"),
                "BLOCKED" => ("Blocked", "pill-red"),
                _ => (string.IsNullOrWhiteSpace(status) ? "Unknown" : status, "pill-gold")
            };
        }
    }
}
