using Airline.Models;
using Microsoft.EntityFrameworkCore;

namespace Airline.Services
{
    public class SeatService
    {
        private readonly DataContext _context;

        public SeatService(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Generates up to 180 seats (30 rows) for a given FlightSchedule based on its TicketPrices.
        /// </summary>
        public async Task GenerateSeatsAsync(int scheduleId)
        {
            var schedule = await _context.FlightSchedules.FindAsync(scheduleId);
            if (schedule == null) return;

            int targetTotalSeats = schedule.TotalSeats ?? 180;
            int totalRows = (int)Math.Ceiling(targetTotalSeats / 6.0);

            // 1. Get currently priced classes for this schedule
            var pricedClassIds = await _context.TicketPrices
                .Where(p => p.ScheduleId == scheduleId)
                .Select(p => p.ClassId)
                .OrderByDescending(id => id) // First (4) down to Economy (1)
                .ToListAsync();

            if (!pricedClassIds.Any())
            {
                // Fallback to Economy if no prices are set yet
                pricedClassIds.Add(1);
            }

            // 2. Get existing seats to check if regeneration is needed
            var existingSeats = await _context.Seats
                .Where(s => s.ScheduleId == scheduleId)
                .ToListAsync();

            if (existingSeats.Any())
            {
                var generatedClassIds = existingSeats.Select(s => s.ClassId).Distinct().OrderByDescending(id => id).ToList();
                
                // If classes match and we have reached target seats, nothing to do
                if (generatedClassIds.SequenceEqual(pricedClassIds) && existingSeats.Count >= targetTotalSeats)
                    return;

                bool anyBooked = existingSeats.Any(s => s.SeatStatus != "AVAILABLE");
                
                // If classes don't match, or seats are missing, verify if we can regenerate
                if (anyBooked) 
                {
                    // If some seats are already booked, we CANNOT safely wipe and regenerate the layout.
                    if (existingSeats.Count >= targetTotalSeats) return;
                }
                else
                {
                    // Wipe all unbooked seats so we can generate the new layout based on updated prices
                    _context.Seats.RemoveRange(existingSeats);
                    await _context.SaveChangesAsync();
                    existingSeats.Clear();
                }
            }

            var existingSeatNumbers = existingSeats.Select(s => s.SeatNumber).ToList();

            var seatsToAdd = new List<Seat>();
            
            // 3. Define Row Distribution based on how many classes are priced
            // Logic: Distribute 30 rows among the available classes
            Dictionary<int, int> rowMapping = new Dictionary<int, int>();
            
            if (pricedClassIds.Count == 4)
            {
                rowMapping[4] = Math.Max(1, (int)Math.Round(totalRows * 0.06));   // First
                rowMapping[3] = Math.Max(1, (int)Math.Round(totalRows * 0.13));   // Business
                rowMapping[2] = Math.Max(1, (int)Math.Round(totalRows * 0.20));   // Premium
                rowMapping[1] = Math.Max(0, totalRows - rowMapping[4] - rowMapping[3] - rowMapping[2]);  // Economy
            }
            else if (pricedClassIds.Count == 3)
            {
                rowMapping[pricedClassIds[0]] = Math.Max(1, (int)Math.Round(totalRows * 0.16)); 
                rowMapping[pricedClassIds[1]] = Math.Max(1, (int)Math.Round(totalRows * 0.26));  
                rowMapping[pricedClassIds[2]] = Math.Max(0, totalRows - rowMapping[pricedClassIds[0]] - rowMapping[pricedClassIds[1]]); 
            }
            else if (pricedClassIds.Count == 2)
            {
                rowMapping[pricedClassIds[0]] = Math.Max(1, (int)Math.Round(totalRows * 0.26));  
                rowMapping[pricedClassIds[1]] = Math.Max(0, totalRows - rowMapping[pricedClassIds[0]]); 
            }
            else // Only 1 class
            {
                rowMapping[pricedClassIds[0]] = totalRows;
            }

            int currentRow = 1;
            foreach (var classId in pricedClassIds)
            {
                int rowsForThisClass = rowMapping.ContainsKey(classId) ? rowMapping[classId] : 0;
                int endRow = Math.Min(currentRow + rowsForThisClass - 1, totalRows);

                for (int r = currentRow; r <= endRow; r++)
                {
                    foreach (var col in new[] { "A", "B", "C", "D", "E", "F" })
                    {
                        if (existingSeats.Count + seatsToAdd.Count >= targetTotalSeats)
                            break;

                        string seatNum = $"{col}{r:D2}";
                        if (!existingSeatNumbers.Contains(seatNum))
                        {
                            seatsToAdd.Add(new Seat 
                            { 
                                ScheduleId = scheduleId, 
                                ClassId = classId, 
                                SeatNumber = seatNum, 
                                SeatStatus = "AVAILABLE" 
                            });
                        }
                    }
                    if (existingSeats.Count + seatsToAdd.Count >= targetTotalSeats)
                        break;
                }
                currentRow = endRow + 1;
            }

            if (seatsToAdd.Any())
            {
                _context.Seats.AddRange(seatsToAdd);
                await _context.SaveChangesAsync();
            }
        }
    }
}
