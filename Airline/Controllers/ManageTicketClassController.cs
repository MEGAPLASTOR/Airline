using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    public class ManageTicketClassController : Controller
    {
        private readonly DataContext _context;

        public ManageTicketClassController(DataContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var isAuth = User?.Identity?.IsAuthenticated == true;
            var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return isAuth && string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult RedirectIfNotAdmin()
        {
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> TicketClasses(string search)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var query = _context.TicketClasses.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(x => x.ClassName.ToLower().Contains(keyword));
            }

            var ticketClasses = await query
                .OrderBy(x => x.ClassName)
                .ToListAsync();

            ViewBag.Search = search;
            return View("~/Views/Admin/TicketClasses.cshtml", ticketClasses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTicketClass(string className)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            className = className?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(className))
            {
                TempData["ErrorMessage"] = "Class name is required.";
                return RedirectToAction(nameof(TicketClasses));
            }

            if (className.Length > 50)
            {
                TempData["ErrorMessage"] = "Class name must not exceed 50 characters.";
                return RedirectToAction(nameof(TicketClasses));
            }

            var isExists = await _context.TicketClasses
                .AnyAsync(x => x.ClassName.ToLower() == className.ToLower());

            if (isExists)
            {
                TempData["ErrorMessage"] = "Ticket class already exists.";
                return RedirectToAction(nameof(TicketClasses));
            }

            try
            {
                _context.TicketClasses.Add(new TicketClass
                {
                    ClassName = className
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ticket class created successfully.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Failed to create ticket class.";
            }

            return RedirectToAction(nameof(TicketClasses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTicketClass(int id, string className)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            className = className?.Trim() ?? string.Empty;

            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid ticket class id.";
                return RedirectToAction(nameof(TicketClasses));
            }

            if (string.IsNullOrWhiteSpace(className))
            {
                TempData["ErrorMessage"] = "Class name is required.";
                return RedirectToAction(nameof(TicketClasses));
            }

            if (className.Length > 50)
            {
                TempData["ErrorMessage"] = "Class name must not exceed 50 characters.";
                return RedirectToAction(nameof(TicketClasses));
            }

            var ticketClass = await _context.TicketClasses
                .FirstOrDefaultAsync(x => x.ClassId == id);

            if (ticketClass == null)
            {
                TempData["ErrorMessage"] = "Ticket class not found.";
                return RedirectToAction(nameof(TicketClasses));
            }

            var isDuplicate = await _context.TicketClasses
                .AnyAsync(x => x.ClassId != id && x.ClassName.ToLower() == className.ToLower());

            if (isDuplicate)
            {
                TempData["ErrorMessage"] = "Ticket class already exists.";
                return RedirectToAction(nameof(TicketClasses));
            }

            try
            {
                ticketClass.ClassName = className;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Ticket class updated successfully.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Failed to update ticket class.";
            }

            return RedirectToAction(nameof(TicketClasses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTicketClass(int id)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid ticket class id.";
                return RedirectToAction(nameof(TicketClasses));
            }

            var ticketClass = await _context.TicketClasses
                .Include(x => x.Tickets)
                .Include(x => x.TicketPrices)
                .FirstOrDefaultAsync(x => x.ClassId == id);

            if (ticketClass == null)
            {
                TempData["ErrorMessage"] = "Ticket class not found.";
                return RedirectToAction(nameof(TicketClasses));
            }

            if (ticketClass.Tickets.Any() || ticketClass.TicketPrices.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete this class because it is already in use.";
                return RedirectToAction(nameof(TicketClasses));
            }

            try
            {
                _context.TicketClasses.Remove(ticketClass);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Ticket class deleted successfully.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Failed to delete ticket class.";
            }

            return RedirectToAction(nameof(TicketClasses));
        }
    }
}