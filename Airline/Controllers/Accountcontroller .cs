using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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
            if (string.IsNullOrWhiteSpace(req.username) || string.IsNullOrWhiteSpace(req.password))
                return Ok(new { success = false, message = "Thiếu dữ liệu" });

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == req.username);

            if (user == null || user.Password != req.password)
                return Ok(new { success = false, message = "Sai tài khoản hoặc mật khẩu" });

            await SignIn(user, req.rememberMe);

            return Ok(new { success = true });
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.username) || string.IsNullOrWhiteSpace(req.password))
                    return Ok(new { success = false, message = "Thiếu dữ liệu" });

                if (await _context.Users.AnyAsync(x => x.Username == req.username))
                    return Ok(new { success = false, message = "Username đã tồn tại" });

                if (!string.IsNullOrEmpty(req.email) &&
                    await _context.Users.AnyAsync(x => x.Email == req.email))
                    return Ok(new { success = false, message = "Email đã tồn tại" });

                if (!string.IsNullOrEmpty(req.cccd) &&
                    await _context.Users.AnyAsync(x => x.Cccd == req.cccd))
                    return Ok(new { success = false, message = "CCCD đã tồn tại" });

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

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
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
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName", user.LastName ?? ""),
                new Claim("SkyMiles", (user.SkyMiles ?? 0).ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe
                });
        }
    }

    public class LoginRequest
    {
        public string username { get; set; }
        public string password { get; set; }
        public bool rememberMe { get; set; }
    }

    public class RegisterRequest
    {
        public string username { get; set; }
        public string password { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string cccd { get; set; }
        public string address { get; set; }
        public string gender { get; set; }
        public int? age { get; set; }
    }
}