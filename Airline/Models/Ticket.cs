using System;
using System.Collections.Generic;

namespace Airline.Models;

public partial class Ticket
{
    public int TicketId { get; set; }
    public int BookingId { get; set; }
    public int PassengerId { get; set; }
    public int ClassId { get; set; }
    public int? SeatId { get; set; }
    public string Status { get; set; } = null!;

    public virtual Booking Booking { get; set; } = null!;
    public virtual Passenger Passenger { get; set; } = null!;
    public virtual TicketClass Class { get; set; } = null!;
    public virtual Seat? Seat { get; set; }
    public virtual ICollection<Baggage> Baggages { get; set; } = new List<Baggage>();
}