using System;
using System.Collections.Generic;

namespace Airline.Models;

public partial class TicketClass
{
    public int ClassId { get; set; }

    public string ClassName { get; set; } = null!;

    public virtual ICollection<TicketPrice> TicketPrices { get; set; } = new List<TicketPrice>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
