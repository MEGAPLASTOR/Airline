using System;
using System.Linq;
using System.Threading.Tasks;
using Airline.Models;
using Airline.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    public sealed class TicketPriceController : Controller
    {
        private readonly DataContext _db;
        private readonly SeatService _seatService;

        public TicketPriceController(DataContext db, SeatService seatService)
        {
            _db = db;
            _seatService = seatService;
        }

        public sealed class TicketPricePageVm
        {
            public int? FilterScheduleId { get; init; }
            public int? FilterClassId { get; init; }
            public ScheduleOptionVm[] Schedules { get; init; } = Array.Empty<ScheduleOptionVm>();
            public ClassOptionVm[] Classes { get; init; } = Array.Empty<ClassOptionVm>();
            public TicketPriceRowVm[] Items { get; init; } = Array.Empty<TicketPriceRowVm>();
        }

        public sealed class ScheduleOptionVm
        {
            public int ScheduleId { get; init; }
            public string Label { get; init; } = "";
        }

        public sealed class ClassOptionVm
        {
            public int ClassId { get; init; }
            public string ClassName { get; init; } = "";
        }

        public sealed class TicketPriceRowVm
        {
            public int PriceId { get; init; }
            public int ScheduleId { get; init; }
            public int ClassId { get; init; }
            public decimal Price { get; init; }
            public string FlightNumber { get; init; } = "";
            public DateTime? DepartureTime { get; init; }
            public DateTime? ArrivalTime { get; init; }
            public string ClassName { get; init; } = "";
        }

        public sealed class TicketPriceUpsertVm
        {
            public int? PriceId { get; init; }
            public int ScheduleId { get; init; }
            public int ClassId { get; init; }
            public decimal Price { get; init; }
            public int? FilterScheduleId { get; init; }
            public int? FilterClassId { get; init; }
        }

        public async Task<IActionResult> TicketPrice(int? scheduleId, int? classId)
        {
            var schedules = await _db.FlightSchedules
                .Include(s => s.Flight)
                .OrderByDescending(s => s.DepartureTime)
                .Select(s => new ScheduleOptionVm
                {
                    ScheduleId = s.ScheduleId,
                    Label = (s.Flight != null ? s.Flight.FlightNumber : "") +
                            " - " + s.DepartureTime.ToString("dd/MM/yyyy HH:mm")
                }).ToArrayAsync();

            var classes = await _db.TicketClasses
                .OrderBy(c => c.ClassName)
                .Select(c => new ClassOptionVm
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName
                }).ToArrayAsync();

            var query = _db.TicketPrices
                .Include(p => p.Schedule).ThenInclude(s => s.Flight)
                .Include(p => p.Class)
                .AsQueryable();

            if (scheduleId.HasValue)
                query = query.Where(x => x.ScheduleId == scheduleId.Value);

            if (classId.HasValue)
                query = query.Where(x => x.ClassId == classId.Value);

            var items = await query
                .OrderByDescending(x => x.PriceId)
                .Select(x => new TicketPriceRowVm
                {
                    PriceId = x.PriceId,
                    ScheduleId = x.ScheduleId,
                    ClassId = x.ClassId,
                    Price = x.Price ?? 0,
                    FlightNumber = x.Schedule.Flight.FlightNumber,
                    DepartureTime = x.Schedule.DepartureTime,
                    ArrivalTime = x.Schedule.ArrivalTime,
                    ClassName = x.Class.ClassName
                }).ToArrayAsync();

            return View("~/Views/Admin/TicketPrice.cshtml", new TicketPricePageVm
            {
                FilterScheduleId = scheduleId,
                FilterClassId = classId,
                Schedules = schedules,
                Classes = classes,
                Items = items
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(TicketPriceUpsertVm vm)
        {
            var dup = await _db.TicketPrices.AnyAsync(x =>
                x.PriceId != (vm.PriceId ?? 0) &&
                x.ScheduleId == vm.ScheduleId &&
                x.ClassId == vm.ClassId);

            if (dup)
            {
                TempData["tp_err"] = "A ticket price for this schedule and class already exists.";
                return RedirectToAction(nameof(TicketPrice));
            }

            if (vm.PriceId.HasValue)
            {
                var e = await _db.TicketPrices.FindAsync(vm.PriceId.Value);
                if (e == null) return NotFound();

                e.ScheduleId = vm.ScheduleId;
                e.ClassId = vm.ClassId;
                e.Price = vm.Price;
            }
            else
            {
                _db.TicketPrices.Add(new TicketPrice
                {
                    ScheduleId = vm.ScheduleId,
                    ClassId = vm.ClassId,
                    Price = vm.Price
                });
            }

            await _db.SaveChangesAsync();
            await _seatService.GenerateSeatsAsync(vm.ScheduleId);
            
            TempData["tp_ok"] = "Ticket price saved successfully.";
            return RedirectToAction(nameof(TicketPrice));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _db.TicketPrices.FindAsync(id);
            if (e == null) return NotFound();

            int scheduleId = e.ScheduleId;
            _db.Remove(e);
            await _db.SaveChangesAsync();
            await _seatService.GenerateSeatsAsync(scheduleId);

            TempData["tp_ok"] = "Ticket price deleted successfully.";
            return RedirectToAction(nameof(TicketPrice));
        }
    }
}
