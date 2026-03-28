using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminCityController : AdminBaseController
    {
        public AdminCityController(DataContext context) : base(context) { }

        [HttpGet("ManageCity")]
        public async Task<IActionResult> ManageCity()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var cities = await _context.Cities
                .OrderBy(x => x.CityName)
                .ToListAsync();

            return View("~/Views/Admin/ManageCity.cshtml", cities);
        }
    }
}
