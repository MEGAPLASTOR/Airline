using System;
using System.Collections.Generic;

namespace Airline.Models;

public partial class FlightSchedule
{
    public int ScheduleId { get; set; }

    public int FlightId { get; set; }

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public int? TotalSeats { get; set; }

    public int? AvailableSeats { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Flight Flight { get; set; } = null!;

    public virtual ICollection<TicketPrice> TicketPrices { get; set; } = new List<TicketPrice>();
}
