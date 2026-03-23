using System;
using System.Collections.Generic;

namespace Airline.Models;

public partial class Route
{
    public int RouteId { get; set; }

    public int DepartureCity { get; set; }

    public int ArrivalCity { get; set; }

    public virtual Cities ArrivalCityNavigation { get; set; } = null!;

    public virtual Cities DepartureCityNavigation { get; set; } = null!;

    public virtual ICollection<Flight> Flights { get; set; } = new List<Flight>();
}
