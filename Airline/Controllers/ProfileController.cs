using Airline.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Airline.Controllers
{
    public class ProfileController : Controller
    {
        private readonly DataContext _context;

        public ProfileController(DataContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────
        //  GET /Account/GetProfile  (dùng cho EditAccount page load)
        // ─────────────────────────────────────────────────────
        [HttpGet("/Account/GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Ok(new { success = false, message = "Not authenticated" });

            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
            if (user == null)
                return Ok(new { success = false, message = "User not found" });

            return Ok(new
            {
                success = true,
                user = new
                {
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Phone,
                    user.Cccd,
                    user.Address,
                    user.Gender,
                    user.Age,
                    user.CreatedAt
                }
            });
        }

        // ─────────────────────────────────────────────────────
        //  POST /Account/ChangePassword
        // ─────────────────────────────────────────────────────
        [HttpPost("/Account/ChangePassword")]
        public async Task<IActionResult> ChangePasswordPost([FromBody] ChangePasswordRequest req)
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Ok(new { success = false, message = "Not authenticated" });

            if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
                return Ok(new { success = false, message = "Thiếu dữ liệu" });

            if (req.NewPassword.Length < 6)
                return Ok(new { success = false, message = "New password must be at least 6 characters" });

            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
            if (user == null)
                return Ok(new { success = false, message = "User not found" });

            if (user.Password != req.CurrentPassword)
                return Ok(new { success = false, message = "Current password is incorrect" });

            user.Password = req.NewPassword;
            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new { success = true });
        }

        // ─────────────────────────────────────────────────────
        //  POST /Account/EditAccount
        // ─────────────────────────────────────────────────────
        [HttpPost("/Account/EditAccount")]
        public async Task<IActionResult> EditAccountPost([FromBody] EditAccountRequest req)
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Ok(new { success = false, message = "Not authenticated" });

            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
            if (user == null)
                return Ok(new { success = false, message = "User not found" });

            // Check email uniqueness (exclude current user)
            if (!string.IsNullOrEmpty(req.Email) &&
                await _context.Users.AnyAsync(x => x.Email == req.Email && x.Username != username))
                return Ok(new { success = false, message = "Email đã tồn tại" });

            // Check CCCD uniqueness (exclude current user)
            if (!string.IsNullOrEmpty(req.Cccd) &&
                await _context.Users.AnyAsync(x => x.Cccd == req.Cccd && x.Username != username))
                return Ok(new { success = false, message = "CCCD đã tồn tại" });

            // Chỉ cập nhật field nào được gửi lên (null/empty → giữ nguyên giá trị cũ)
            if (!string.IsNullOrWhiteSpace(req.FirstName)) user.FirstName = req.FirstName;
            if (!string.IsNullOrWhiteSpace(req.LastName)) user.LastName = req.LastName;
            if (!string.IsNullOrWhiteSpace(req.Email)) user.Email = req.Email;
            if (!string.IsNullOrWhiteSpace(req.Phone)) user.Phone = req.Phone;
            if (!string.IsNullOrWhiteSpace(req.Cccd)) user.Cccd = req.Cccd;
            if (!string.IsNullOrWhiteSpace(req.Address)) user.Address = req.Address;
            if (!string.IsNullOrWhiteSpace(req.Gender)) user.Gender = req.Gender;
            if (req.Age.HasValue) user.Age = req.Age;

            await _context.SaveChangesAsync();

            // Re-issue cookie để claims (FirstName, LastName) cập nhật ngay
            await SignIn(user);

            return Ok(new { success = true });
        }

        // ─────────────────────────────────────────────────────
        //  Helper: re-issue auth cookie
        // ─────────────────────────────────────────────────────
        private async Task SignIn(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName",  user.LastName  ?? ""),
                new Claim("SkyMiles",  (user.SkyMiles ?? 0).ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });
        }
    }

    // ─────────────────────────────────────────────────────
    //  Request models  —  [JsonPropertyName] khớp với
    //  snake_case mà JS gửi lên (first_name, last_name…)
    // ─────────────────────────────────────────────────────

    public class ChangePasswordRequest
    {
        [JsonPropertyName("currentPassword")]
        public string CurrentPassword { get; set; }

        [JsonPropertyName("newPassword")]
        public string NewPassword { get; set; }
    }

    public class EditAccountRequest
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("cccd")]
        public string Cccd { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("gender")]
        public string Gender { get; set; }

        [JsonPropertyName("age")]
        public int? Age { get; set; }
    }
}