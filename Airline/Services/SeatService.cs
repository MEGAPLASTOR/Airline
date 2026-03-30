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
        /// Generates seats for a given FlightSchedule based on its TicketPrices and TotalSeats.
        /// </summary>
        public async Task GenerateSeatsAsync(int scheduleId)
        {
            var schedule = await _context.FlightSchedules
                .Include(s => s.Seats)
                .Include(s => s.TicketPrices)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

            if (schedule == null) return;

            // 1. Requirement: A flight must have prices to have seats generated/visible
            var pricedClassIds = schedule.TicketPrices
                .Select(p => p.ClassId)
                .OrderByDescending(id => id) // First (4) down to Economy (1)
                .ToList();

            if (!pricedClassIds.Any())
            {
                // If no prices are set, we don't generate or update seats.
                // Optionally: If seats exist but price was removed, we could clear them, 
                // but let's just return for now as the flight is hidden from search anyway.
                return;
            }

            int targetTotalSeats = schedule.TotalSeats ?? 180;
            int totalRows = (int)Math.Ceiling(targetTotalSeats / 6.0);

            // 2. Define Row Distribution
            Dictionary<int, int> rowMapping = new Dictionary<int, int>();
            if (pricedClassIds.Count == 4)
            {
                rowMapping[4] = Math.Max(1, (int)Math.Round(totalRows * 0.06));
                rowMapping[3] = Math.Max(1, (int)Math.Round(totalRows * 0.13));
                rowMapping[2] = Math.Max(1, (int)Math.Round(totalRows * 0.20));
                rowMapping[1] = Math.Max(0, totalRows - rowMapping[4] - rowMapping[3] - rowMapping[2]);
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
            else
            {
                rowMapping[pricedClassIds[0]] = totalRows;
            }

            var existingSeats = schedule.Seats.ToList();
            var seatsToAdd = new List<Seat>();
            var seatsToUpdate = new List<Seat>();

            // 3. Iterate through target layout
            int currentRow = 1;
            int seatsProcessed = 0;

            foreach (var classId in pricedClassIds)
            {
                int rowsForThisClass = rowMapping.ContainsKey(classId) ? rowMapping[classId] : 0;
                int endRow = Math.Min(currentRow + rowsForThisClass - 1, totalRows);

                for (int r = currentRow; r <= endRow; r++)
                {
                    foreach (var col in new[] { "A", "B", "C", "D", "E", "F" })
                    {
                        if (seatsProcessed >= targetTotalSeats) break;

                        string seatNum = $"{col}{r:D2}";
                        var existing = existingSeats.FirstOrDefault(s => s.SeatNumber == seatNum);

                        if (existing != null)
                        {
                            // Update class if it's different (e.g., admin changed price distribution)
                            if (existing.ClassId != classId)
                            {
                                // We update it even if booked to keep the seat map physically consistent,
                                // but usually, we'd only update AVAILABLE ones. 
                                // Here we update both to ensure the class-color matches the current pricing.
                                existing.ClassId = classId;
                                seatsToUpdate.Add(existing);
                            }
                        }
                        else
                        {
                            seatsToAdd.Add(new Seat
                            {
                                ScheduleId = scheduleId,
                                ClassId = classId,
                                SeatNumber = seatNum,
                                SeatStatus = "AVAILABLE"
                            });
                        }
                        seatsProcessed++;
                    }
                    if (seatsProcessed >= targetTotalSeats) break;
                }
                currentRow = endRow + 1;
            }

            // 4. Handle Shrinkage (If TotalSeats decreased)
            var seatsToRemove = existingSeats
                .Where(s => !seatsToUpdate.Contains(s) && 
                            pricedClassIds.Contains(s.ClassId) == false || // Logic: if not processed in loop
                            int.Parse(s.SeatNumber.Substring(1)) > totalRows) // Simple row bound check
                .Where(s => s.SeatStatus == "AVAILABLE") // ONLY remove available ones to avoid breaking tickets
                .ToList();

            // Refined removal: any seat whose seat number wasn't "processed" and is available
            var allProcessedNumbers = new HashSet<string>(seatsToAdd.Select(s => s.SeatNumber).Concat(existingSeats.Where(s => !seatsToRemove.Contains(s)).Select(s => s.SeatNumber)));
            var extraSeats = existingSeats.Where(s => !allProcessedNumbers.Contains(s.SeatNumber) && s.SeatStatus == "AVAILABLE").ToList();

            if (seatsToAdd.Any()) _context.Seats.AddRange(seatsToAdd);
            if (extraSeats.Any()) _context.Seats.RemoveRange(extraSeats);
            
            await _context.SaveChangesAsync();
        }
    }
}
