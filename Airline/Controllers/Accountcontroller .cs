using Airline.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Airline.Controllers
{
    [Route("Auth")]
    public class AccountController : Controller
    {
        private readonly DataContext _context;

        public AccountController(DataContext context)
        {
            _context = context;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (req == null ||
                string.IsNullOrWhiteSpace(req.username) ||
                string.IsNullOrWhiteSpace(req.password))
            {
                return Ok(new
                {
                    success = false,
                    message = "Thiếu dữ liệu"
                });
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == req.username);

            if (user == null || user.Password != req.password)
            {
                return Ok(new
                {
                    success = false,
                    message = "Sai tài khoản hoặc mật khẩu"
                });
            }

            await SignIn(user, req.rememberMe);

            var isAdmin = !string.IsNullOrWhiteSpace(user.Role) &&
                          user.Role.Trim().Equals("ADMIN", StringComparison.OrdinalIgnoreCase);

            return Ok(new
            {
                success = true,
                redirectUrl = isAdmin ? "/Auth/AdminDashboard" : "/"
            });
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            try
            {
                if (req == null ||
                    string.IsNullOrWhiteSpace(req.username) ||
                    string.IsNullOrWhiteSpace(req.password))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Thiếu dữ liệu"
                    });
                }

                if (await _context.Users.AnyAsync(x => x.Username == req.username))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Username đã tồn tại"
                    });
                }

                if (!string.IsNullOrWhiteSpace(req.email) &&
                    await _context.Users.AnyAsync(x => x.Email == req.email))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Email đã tồn tại"
                    });
                }

                if (!string.IsNullOrWhiteSpace(req.cccd) &&
                    await _context.Users.AnyAsync(x => x.Cccd == req.cccd))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "CCCD đã tồn tại"
                    });
                }

                var user = new User
                {
                    Username = req.username,
                    Password = req.password,
                    FirstName = req.first_name,
                    LastName = req.last_name,
                    Email = req.email,
                    Phone = req.phone,
                    Cccd = req.cccd,
                    Address = req.address,
                    Gender = req.gender,
                    Age = req.age,
                    Role = "USER",
                    SkyMiles = 0,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                await SignIn(user, true);

                return Ok(new
                {
                    success = true
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("AdminDashboard")]
        public async Task<IActionResult> AdminDashboard()
        {
            var isAuth = User?.Identity?.IsAuthenticated == true;
            var role = User?.FindFirst(ClaimTypes.Role)?.Value;

            if (!isAuth || !string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Home");
            }

            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var today = now.Date;
            var tomorrow = today.AddDays(1);

            var firstName = User?.FindFirst("FirstName")?.Value ?? "Admin";

            var totalUsers = await _context.Users.CountAsync();

            var totalFlights = await _context.Flights.CountAsync();

            var activeBookings = await _context.Bookings
                .CountAsync(x => x.Status != null && x.Status.ToUpper() == "ACTIVE");

            var revenueThisMonth = await _context.Payments
                .Where(x =>
                    x.PaymentDate != null &&
                    x.PaymentDate >= monthStart &&
                    x.PaymentDate < monthEnd &&
                    x.Amount != null &&
                    (x.PaymentStatus == null || x.PaymentStatus.ToUpper() == "PAID"))
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var scheduledFlightsToday = await _context.FlightSchedules
                .CountAsync(x => x.DepartureTime >= today && x.DepartureTime < tomorrow);

            var availableSeatsToday = await _context.FlightSchedules
                .Where(x => x.DepartureTime >= today && x.DepartureTime < tomorrow)
                .SumAsync(x => (int?)x.AvailableSeats) ?? 0;

            var ticketClassDistribution = await
                (from t in _context.Tickets
                 join tc in _context.TicketClasses on t.ClassId equals tc.ClassId
                 group t by tc.ClassName into g
                 orderby g.Count() descending
                 select new TicketClassChartItem
                 {
                     ClassName = g.Key,
                     Count = g.Count()
                 }).ToListAsync();

            var topRoutes = await
                (from t in _context.Tickets
                 join b in _context.Bookings on t.BookingId equals b.BookingId
                 join fs in _context.FlightSchedules on b.ScheduleId equals fs.ScheduleId
                 join f in _context.Flights on fs.FlightId equals f.FlightId
                 join r in _context.Routes on f.RouteId equals r.RouteId
                 join dep in _context.Cities on r.DepartureCity equals dep.CityId
                 join arr in _context.Cities on r.ArrivalCity equals arr.CityId
                 group t by new { dep.CityName, arr.CityName } into g
                 orderby g.Count() descending
                 select new TopRouteItem
                 {
                     RouteName = g.Key.CityName + " → " + g.Key.CityName1,
                     PassengerCount = g.Count()
                 })
                .Take(5)
                .ToListAsync();

            var recentTickets = await
                (from t in _context.Tickets
                 join p in _context.Passengers on t.PassengerId equals p.PassengerId
                 join tc in _context.TicketClasses on t.ClassId equals tc.ClassId
                 join b in _context.Bookings on t.BookingId equals b.BookingId
                 join fs in _context.FlightSchedules on b.ScheduleId equals fs.ScheduleId
                 join f in _context.Flights on fs.FlightId equals f.FlightId
                 join r in _context.Routes on f.RouteId equals r.RouteId
                 join dep in _context.Cities on r.DepartureCity equals dep.CityId
                 join arr in _context.Cities on r.ArrivalCity equals arr.CityId
                 orderby t.TicketId descending
                 select new RecentTicketActivityItem
                 {
                     TicketId = t.TicketId,
                     PassengerName = p.FullName,
                     RouteName = dep.CityName + " → " + arr.CityName,
                     ClassName = tc.ClassName,
                     Status = t.Status ?? "UNKNOWN"
                 })
                .Take(10)
                .ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                FirstName = firstName,
                TotalUsers = totalUsers,
                TotalFlights = totalFlights,
                ActiveBookings = activeBookings,
                RevenueThisMonth = revenueThisMonth,
                ScheduledFlightsToday = scheduledFlightsToday,
                AvailableSeatsToday = availableSeatsToday,
                TicketClassDistribution = ticketClassDistribution,
                TopRoutes = topRoutes,
                RecentTickets = recentTickets
            };

            return View(vm);
        }

        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("/Account/ChangePassword")]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpGet("/Account/EditAccount")]
        public IActionResult EditAccount()
        {
            return View();
        }

        private async Task SignIn(User user, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName", user.LastName ?? ""),
                new Claim("SkyMiles", (user.SkyMiles ?? 0).ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "")
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe
                }
            );
        }
    }

    public class LoginRequest
    {
        public string username { get; set; } = "";
        public string password { get; set; } = "";
        public bool rememberMe { get; set; }
    }

    public class RegisterRequest
    {
        public string username { get; set; } = "";
        public string password { get; set; } = "";
        public string first_name { get; set; } = "";
        public string last_name { get; set; } = "";
        public string email { get; set; } = "";
        public string phone { get; set; } = "";
        public string cccd { get; set; } = "";
        public string address { get; set; } = "";
        public string gender { get; set; } = "";
        public int? age { get; set; }
    }
}