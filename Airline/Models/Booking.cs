using System;
using System.Collections.Generic;

namespace Airline.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int UserId { get; set; }

    public int ScheduleId { get; set; }

    public string BookingType { get; set; } = null!;

    public DateTime? BookingDate { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<BookingPromotion> BookingPromotions { get; set; } = new List<BookingPromotion>();

    public virtual ICollection<Passenger> Passengers { get; set; } = new List<Passenger>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual FlightSchedule Schedule { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual User User { get; set; } = null!;
}
