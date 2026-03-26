using System;
using System.Collections.Generic;

namespace Airline.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Gender { get; set; }

    public int? Age { get; set; }

    public string Role { get; set; } = null!;

    public int? SkyMiles { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Cccd { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
