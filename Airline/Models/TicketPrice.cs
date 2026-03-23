using System;
using System.Collections.Generic;

namespace Airline.Models;

public partial class TicketPrice
{
    public int PriceId { get; set; }

    public int ScheduleId { get; set; }

    public int ClassId { get; set; }

    public decimal? Price { get; set; }

    public virtual TicketClass Class { get; set; } = null!;

    public virtual FlightSchedule Schedule { get; set; } = null!;
}
