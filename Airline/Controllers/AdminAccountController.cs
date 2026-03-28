using Airline.Models;
using Airline.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminAccountController : AdminBaseController
    {
        public AdminAccountController(DataContext context) : base(context) { }

        [HttpGet("ManageAccounts")]
        public async Task<IActionResult> ManageAccounts()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var accounts = await _context.Users
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.UserId)
                .ToListAsync();

            var viewModel = new ManageAccountsViewModel
            {
                Accounts = accounts
            };

            return View("~/Views/Admin/ManageAccounts.cshtml", viewModel);
        }

        [HttpPost("CreateAccount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(AccountFormModel model)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid form data.";
                return RedirectToAction(nameof(ManageAccounts));
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                TempData["ErrorMessage"] = "Password is required when creating a new account.";
                return RedirectToAction(nameof(ManageAccounts));
            }

            var normalizedUsername = model.Username.Trim();
            var normalizedEmail = model.Email.Trim();
            var normalizedCccd = model.Cccd?.Trim();

            if (await _context.Users.AnyAsync(x => x.Username == normalizedUsername))
            {
                TempData["ErrorMessage"] = "Username already exists.";
                return RedirectToAction(nameof(ManageAccounts));
            }

            if (await _context.Users.AnyAsync(x => x.Email == normalizedEmail))
            {
                TempData["ErrorMessage"] = "Email already exists.";
                return RedirectToAction(nameof(ManageAccounts));
            }

            if (!string.IsNullOrWhiteSpace(normalizedCccd) &&
                await _context.Users.AnyAsync(x => x.Cccd == normalizedCccd))
            {
                TempData["ErrorMessage"] = "CCCD already exists.";
                return RedirectToAction(nameof(ManageAccounts));
            }

            var user = new User
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Username = normalizedUsername,
                Email = normalizedEmail,
                Phone = model.Phone?.Trim(),
                Address = model.Address?.Trim(),
                Gender = model.Gender?.Trim(),
                Age = model.Age,
                Cccd = normalizedCccd,
                Password = model.Password.Trim(),
                Role = string.IsNullOrWhiteSpace(model.Role) ? "USER" : model.Role.Trim().ToUpper(),
                SkyMiles = model.SkyMiles ?? 0,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Account created successfully.";
            return RedirectToAction(nameof(ManageAccounts));
        }

        [HttpPost("UpdateAccount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAccount(AccountFormModel model)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            if (model.UserId == null)
            {
                TempData["ErrorMessage"] = "User ID is invalid.";
                return RedirectToAction(nameof(ManageAccounts));
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid form data.";
                return RedirectToAction(nameof(ManageAccounts));
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == model.UserId.Value);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Account not found.";
                return RedirectToAction(nameof(ManageAccounts));
            }

            var normalizedUsername = model.Username.Trim();
            var normalizedEmail = model.Email.Trim();
            var normalizedCccd = model.Cccd?.Trim();

            if (await _context.Users.AnyAsync(x => x.UserId != user.UserId && x.Username == normalizedUsername))
            {
                TempData["ErrorMessage"] = "Username already exists.";
                return RedirectToAction(nameof(ManageAccounts));
            }

            if (await _context.Users.AnyAsync(x => x.UserId != user.UserId && x.Email == normalizedEmail))
            {
                TempData["ErrorMessage"] = "Email already exists.";
                return RedirectToAction(nameof(ManageAccounts));
            }

            if (!string.IsNullOrWhiteSpace(normalizedCccd) &&
                await _context.Users.AnyAsync(x => x.UserId != user.UserId && x.Cccd == normalizedCccd))
            {
                TempData["ErrorMessage"] = "CCCD already exists.";
                return RedirectToAction(nameof(ManageAccounts));
            }

            user.FirstName = model.FirstName.Trim();
            user.LastName = model.LastName.Trim();
            user.Username = normalizedUsername;
            user.Email = normalizedEmail;
            user.Phone = model.Phone?.Trim();
            user.Address = model.Address?.Trim();
            user.Gender = model.Gender?.Trim();
            user.Age = model.Age;
            user.Cccd = normalizedCccd;
            user.Role = string.IsNullOrWhiteSpace(model.Role) ? "USER" : model.Role.Trim().ToUpper();
            user.SkyMiles = model.SkyMiles ?? 0;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.Password = model.Password.Trim();
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Account updated successfully.";
            return RedirectToAction(nameof(ManageAccounts));
        }

        [HttpPost("DeleteAccount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(int userId)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Account not found.";
                return RedirectToAction(nameof(ManageAccounts));
            }

            var currentUsername = User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(currentUsername) &&
                string.Equals(user.Username, currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "You cannot delete the currently logged in account.";
                return RedirectToAction(nameof(ManageAccounts));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Account deleted successfully.";
            return RedirectToAction(nameof(ManageAccounts));
        }
    }
}
