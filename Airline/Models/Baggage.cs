using System;
using System.Collections.Generic;

namespace Airline.Models;

public partial class Baggage
{
    public int BaggageId { get; set; }

    public int TicketId { get; set; }

    public decimal? Weight { get; set; }

    public decimal? Price { get; set; }

    public virtual Ticket Ticket { get; set; } = null!;
}
