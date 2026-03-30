using System;
using System.Collections.Generic;

namespace Airline.Models;

public partial class Seat
{
    public int SeatId { get; set; }
    public int ScheduleId { get; set; }
    public int ClassId { get; set; }
    public string SeatNumber { get; set; } = null!;
    public string SeatStatus { get; set; } = null!;

    public virtual FlightSchedule Schedule { get; set; } = null!;
    public virtual TicketClass Class { get; set; } = null!;
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}