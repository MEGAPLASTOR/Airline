using System;
using System.Collections.Generic;

namespace Airline.Models;

public partial class Flight
{
    public int FlightId { get; set; }

    public string FlightNumber { get; set; } = null!;

    public int RouteId { get; set; }

    public virtual ICollection<FlightSchedule> FlightSchedules { get; set; } = new List<FlightSchedule>();

    public virtual Route Route { get; set; } = null!;
}
