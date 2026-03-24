using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace Airline.Controllers
{
    public class AdminController : Controller
    {
        private readonly DataContext _context;

        public AdminController(DataContext context)
        {
            _context = context;
        }

        // Authentication Helper
        private bool IsAdmin()
        {
            var isAuth = User?.Identity?.IsAuthenticated == true;
            var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return isAuth && string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);
        }

        // ==========================================
        // ADMIN DASHBOARD (Overview)
        // ==========================================
        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            return View("AdminDashboard");
        }

        // ==========================================
        // MANAGE CITY API / View
        // ==========================================
        public IActionResult ManageCity(string search)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var query = _context.Cities
                .Include(c => c.RouteDepartureCityNavigations)
                .Include(c => c.RouteArrivalCityNavigations)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(c => c.CityName.ToLower().Contains(lowerSearch) ||
                                         c.Country.ToLower().Contains(lowerSearch));
            }

            ViewBag.Search = search;
            return View(query.ToList());
        }

        [HttpGet]
        public IActionResult GetCity(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            var city = _context.Cities.Find(id);
            if (city == null) return NotFound();
            return Json(new { cityId = city.CityId, cityName = city.CityName, country = city.Country });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCity(string cityName, string country)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            if (string.IsNullOrWhiteSpace(cityName) || string.IsNullOrWhiteSpace(country))
                return Json(new { success = false, message = "City name and country are required." });

            try
            {
                _context.Cities.Add(new Cities { CityName = cityName.Trim(), Country = country.Trim() });
                _context.SaveChanges();
                return Json(new { success = true, message = "City added successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while adding the city." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCity(int id, string cityName, string country)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            var city = _context.Cities.Find(id);
            if (city == null) return Json(new { success = false, message = "City not found." });

            if (string.IsNullOrWhiteSpace(cityName) || string.IsNullOrWhiteSpace(country))
                return Json(new { success = false, message = "City name and country are required." });

            try
            {
                city.CityName = cityName.Trim();
                city.Country = country.Trim();
                _context.SaveChanges();
                return Json(new { success = true, message = "City updated successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while updating the city." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCity(int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            var city = _context.Cities
                .Include(c => c.RouteDepartureCityNavigations)
                .Include(c => c.RouteArrivalCityNavigations)
                .FirstOrDefault(c => c.CityId == id);

            if (city == null) return Json(new { success = false, message = "City not found." });

            // Ensure no routes use this city
            if (city.RouteDepartureCityNavigations.Any() || city.RouteArrivalCityNavigations.Any())
                return Json(new { success = false, message = "Cannot delete city because it is part of existing routes." });

            try
            {
                _context.Cities.Remove(city);
                _context.SaveChanges();
                return Json(new { success = true, message = "City deleted successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while deleting the city." });
            }
        }

        // ==========================================
        // MANAGE ROUTE API / View
        // ==========================================
        public IActionResult ManageRoutes(string search)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var query = _context.Routes
                .Include(r => r.DepartureCityNavigation)
                .Include(r => r.ArrivalCityNavigation)
                .Include(r => r.Flights)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(r => r.DepartureCityNavigation.CityName.ToLower().Contains(lowerSearch) ||
                                         r.ArrivalCityNavigation.CityName.ToLower().Contains(lowerSearch));
            }

            ViewBag.Cities = _context.Cities.OrderBy(c => c.CityName).ToList();
            ViewBag.Search = search;
            return View(query.ToList());
        }

        [HttpGet]
        public IActionResult GetRoute(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            var r = _context.Routes.Find(id);
            if (r == null) return NotFound();
            return Json(new { routeId = r.RouteId, departureCity = r.DepartureCity, arrivalCity = r.ArrivalCity });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateRoute(int departureCity, int arrivalCity)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            if (departureCity == arrivalCity)
                return Json(new { success = false, message = "Departure and arrival cities cannot be the same." });

            // Check if route already exists
            if (_context.Routes.Any(r => r.DepartureCity == departureCity && r.ArrivalCity == arrivalCity))
                return Json(new { success = false, message = "This route already exists." });

            try
            {
                _context.Routes.Add(new Models.Route { DepartureCity = departureCity, ArrivalCity = arrivalCity });
                _context.SaveChanges();
                return Json(new { success = true, message = "Route added successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while adding the route." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditRoute(int id, int departureCity, int arrivalCity)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            var route = _context.Routes.Find(id);
            if (route == null) return Json(new { success = false, message = "Route not found." });

            if (departureCity == arrivalCity)
                return Json(new { success = false, message = "Departure and arrival cities cannot be the same." });

            // Check duplicate
            if (_context.Routes.Any(r => r.RouteId != id && r.DepartureCity == departureCity && r.ArrivalCity == arrivalCity))
                return Json(new { success = false, message = "This route already exists." });

            try
            {
                route.DepartureCity = departureCity;
                route.ArrivalCity = arrivalCity;
                _context.SaveChanges();
                return Json(new { success = true, message = "Route updated successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while updating the route." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteRoute(int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            var route = _context.Routes
                .Include(r => r.Flights)
                .FirstOrDefault(r => r.RouteId == id);

            if (route == null) return Json(new { success = false, message = "Route not found." });

            if (route.Flights.Any())
                return Json(new { success = false, message = "Cannot delete route because there are flights assigned to it." });

            try
            {
                _context.Routes.Remove(route);
                _context.SaveChanges();
                return Json(new { success = true, message = "Route deleted successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while deleting the route." });
            }
        }

        // ==========================================
        // MANAGE FLIGHTS API / View
        // ==========================================
        public IActionResult ManageFlights(string search)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var query = _context.Flights
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureCityNavigation)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalCityNavigation)
                .Include(f => f.FlightSchedules)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(f => f.FlightNumber.ToLower().Contains(lowerSearch) ||
                                         (f.Route.DepartureCityNavigation.CityName + " " + f.Route.ArrivalCityNavigation.CityName).ToLower().Contains(lowerSearch));
            }

            ViewBag.Routes = _context.Routes
                .Include(r => r.DepartureCityNavigation)
                .Include(r => r.ArrivalCityNavigation)
                .ToList();
            ViewBag.Search = search;

            return View(query.ToList());
        }

        [HttpGet]
        public IActionResult GetFlight(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            var f = _context.Flights.Find(id);
            if (f == null) return NotFound();
            return Json(new { flightId = f.FlightId, flightNumber = f.FlightNumber, routeId = f.RouteId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateFlight(string flightNumber, int routeId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            if (string.IsNullOrWhiteSpace(flightNumber))
                return Json(new { success = false, message = "Flight number is required." });

            flightNumber = flightNumber.Trim().ToUpper();

            if (_context.Flights.Any(f => f.FlightNumber.ToUpper() == flightNumber))
                return Json(new { success = false, message = "Flight number already exists." });

            try
            {
                _context.Flights.Add(new Flight { FlightNumber = flightNumber, RouteId = routeId });
                _context.SaveChanges();
                return Json(new { success = true, message = "Flight added successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while adding the flight." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditFlight(int id, string flightNumber, int routeId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            var flight = _context.Flights.Find(id);
            if (flight == null) return Json(new { success = false, message = "Flight not found." });

            if (string.IsNullOrWhiteSpace(flightNumber))
                return Json(new { success = false, message = "Flight number is required." });

            flightNumber = flightNumber.Trim().ToUpper();

            if (_context.Flights.Any(f => f.FlightId != id && f.FlightNumber.ToUpper() == flightNumber))
                return Json(new { success = false, message = "Flight number already exists." });

            try
            {
                flight.FlightNumber = flightNumber;
                flight.RouteId = routeId;
                _context.SaveChanges();
                return Json(new { success = true, message = "Flight updated successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while updating the flight." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteFlight(int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            var flight = _context.Flights
                .Include(f => f.FlightSchedules)
                .FirstOrDefault(f => f.FlightId == id);

            if (flight == null) return Json(new { success = false, message = "Flight not found." });

            if (flight.FlightSchedules.Any())
                return Json(new { success = false, message = "Cannot delete flight because schedules are assigned to it." });

            try
            {
                _context.Flights.Remove(flight);
                _context.SaveChanges();
                return Json(new { success = true, message = "Flight deleted successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while deleting the flight." });
            }
        }

        // ==========================================
        // FLIGHT SCHEDULES (CRUD for specific times)
        // ==========================================
        public IActionResult FlightSchedules(string search, string status, string dateFrom, string dateTo)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var query = _context.FlightSchedules
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.ArrivalCityNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(s => s.Flight.FlightNumber.ToLower().Contains(lowerSearch) ||
                                         s.Flight.Route.DepartureCityNavigation.CityName.ToLower().Contains(lowerSearch) ||
                                         s.Flight.Route.ArrivalCityNavigation.CityName.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrEmpty(status) && status != "ALL")
            {
                query = query.Where(s => s.Status == status);
            }

            if (DateTime.TryParse(dateFrom, out DateTime fromDate))
            {
                query = query.Where(s => s.DepartureTime >= fromDate);
            }

            if (DateTime.TryParse(dateTo, out DateTime toDate))
            {
                var endOfDay = toDate.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.DepartureTime <= endOfDay);
            }

            ViewBag.Flights = _context.Flights
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureCityNavigation)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalCityNavigation)
                .ToList();

            ViewBag.Search = search;
            ViewBag.StatusFilter = status;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            return View(query.OrderBy(s => s.DepartureTime).ToList());
        }

        [HttpGet]
        public IActionResult GetSchedule(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            var s = _context.FlightSchedules.Find(id);
            if (s == null) return NotFound();
            return Json(new
            {
                scheduleId = s.ScheduleId,
                flightId = s.FlightId,
                departureTime = s.DepartureTime.ToString("yyyy-MM-ddTHH:mm"),
                arrivalTime = s.ArrivalTime.ToString("yyyy-MM-ddTHH:mm"),
                totalSeats = s.TotalSeats,
                availableSeats = s.AvailableSeats,
                status = s.Status
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateSchedule(int flightId, DateTime departureTime, DateTime arrivalTime, int totalSeats, string status)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            if (departureTime >= arrivalTime)
                return Json(new { success = false, message = "Arrival time must be after departure time." });

            if (totalSeats <= 0)
                return Json(new { success = false, message = "Total seats must be greater than 0." });

            try
            {
                var sch = new FlightSchedule
                {
                    FlightId = flightId,
                    DepartureTime = departureTime,
                    ArrivalTime = arrivalTime,
                    TotalSeats = totalSeats,
                    AvailableSeats = totalSeats, // Initially all seats available
                    Status = status
                };
                _context.FlightSchedules.Add(sch);
                _context.SaveChanges();
                return Json(new { success = true, message = "Schedule added successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while adding the schedule." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditSchedule(int id, int flightId, DateTime departureTime, DateTime arrivalTime, int totalSeats, int availableSeats, string status)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            var sch = _context.FlightSchedules.Find(id);
            if (sch == null) return Json(new { success = false, message = "Schedule not found." });

            if (departureTime >= arrivalTime)
                return Json(new { success = false, message = "Arrival time must be after departure time." });

            if (totalSeats <= 0)
                return Json(new { success = false, message = "Total seats must be greater than 0." });

            if (availableSeats > totalSeats || availableSeats < 0)
                return Json(new { success = false, message = "Invalid available seats value." });

            try
            {
                sch.FlightId = flightId;
                sch.DepartureTime = departureTime;
                sch.ArrivalTime = arrivalTime;
                sch.TotalSeats = totalSeats;
                sch.AvailableSeats = availableSeats;
                sch.Status = status;
                _context.SaveChanges();
                return Json(new { success = true, message = "Schedule updated successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while updating the schedule." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteSchedule(int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            var sch = _context.FlightSchedules.Find(id);
            if (sch == null) return Json(new { success = false, message = "Schedule not found." });

            // Check if there are bookings for this schedule (Assuming a Bookings table relation exists in a full app)
            // if (sch.TotalSeats != sch.AvailableSeats)
            //    return Json(new { success = false, message = "Cannot delete schedule because tickets have been booked." });

            try
            {
                _context.FlightSchedules.Remove(sch);
                _context.SaveChanges();
                return Json(new { success = true, message = "Schedule deleted successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while deleting the schedule." });
            }
        }

        // ==========================================
        // FLIGHT RESCHEDULE (Dedicated operational view)
        // ==========================================
        public IActionResult FlightReschedule(string search, string status, string filterDate)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var query = _context.FlightSchedules
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.ArrivalCityNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(s => s.Flight.FlightNumber.ToLower().Contains(lowerSearch) ||
                                         s.Flight.Route.DepartureCityNavigation.CityName.ToLower().Contains(lowerSearch) ||
                                         s.Flight.Route.ArrivalCityNavigation.CityName.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrEmpty(status) && status != "ALL")
            {
                query = query.Where(s => s.Status == status);
            }

            if (DateTime.TryParse(filterDate, out DateTime fDate))
            {
                var endOfDay = fDate.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.DepartureTime >= fDate && s.DepartureTime <= endOfDay);
            }

            ViewBag.Search = search;
            ViewBag.StatusFilter = status;
            ViewBag.FilterDate = filterDate;

            return View(query.OrderByDescending(s => s.DepartureTime).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessReschedule(int id, DateTime newDepartureTime, DateTime newArrivalTime, string status)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            var sch = _context.FlightSchedules.Find(id);
            if (sch == null) return Json(new { success = false, message = "Flight schedule not found." });

            if (newDepartureTime >= newArrivalTime)
                return Json(new { success = false, message = "Arrival time must be after departure time." });

            try
            {
                sch.DepartureTime = newDepartureTime;
                sch.ArrivalTime = newArrivalTime;
                sch.Status = status; // Usually updated to 'DELAYED'
                _context.SaveChanges();
                return Json(new { success = true, message = "Flight has been successfully rescheduled." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while rescheduling the flight." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelFlightSchedule(int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized access." });

            var sch = _context.FlightSchedules.Find(id);
            if (sch == null) return Json(new { success = false, message = "Flight schedule not found." });

            if (sch.Status == "COMPLETED")
                return Json(new { success = false, message = "Cannot cancel a completed flight." });

            try
            {
                sch.Status = "CANCELLED";
                _context.SaveChanges();
                return Json(new { success = true, message = "Flight has been cancelled." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while cancelling the flight." });
            }
        }
    }
}
