using System;
using System.Collections.Generic;

namespace Airline.Models;

public partial class Cities
{
    public int CityId { get; set; }

    public string CityName { get; set; } = null!;

    public string Country { get; set; } = null!;

    public virtual ICollection<Route> RouteArrivalCityNavigations { get; set; } = new List<Route>();

    public virtual ICollection<Route> RouteDepartureCityNavigations { get; set; } = new List<Route>();
}
