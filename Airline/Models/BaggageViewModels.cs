using System.ComponentModel.DataAnnotations;

namespace Airline.Models;

public sealed class BaggageRegisterInputModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid ticket.")]
    public int TicketId { get; set; }

    [Required(ErrorMessage = "Please enter a baggage weight.")]
    [Range(typeof(decimal), "1", "99.99", ErrorMessage = "Weight must be between 1 and 99.99 kg.")]
    public decimal? Weight { get; set; }
}

public sealed class BaggageUpdateWeightModel
{
    [Range(1, int.MaxValue, ErrorMessage = "The baggage record is invalid.")]
    public int BaggageId { get; set; }

    [Required(ErrorMessage = "Please enter a baggage weight.")]
    [Range(typeof(decimal), "1", "99.99", ErrorMessage = "Weight must be between 1 and 99.99 kg.")]
    public decimal? Weight { get; set; }
}

public sealed class BaggageTicketOptionViewModel
{
    public int TicketId { get; init; }

    public int BookingId { get; init; }

    public int UserId { get; init; }

    public string CustomerName { get; init; } = "";

    public string Username { get; init; } = "";

    public string PassengerName { get; init; } = "";

    public string FlightNumber { get; init; } = "";

    public string RouteLabel { get; init; } = "";

    public DateTime? DepartureTime { get; init; }

    public string TicketStatus { get; init; } = "";

    public string DisplayText =>
        $"Ticket #{TicketId} | {PassengerName} | {FlightNumber} | {RouteLabel}";
}

public sealed class UserBaggageItemViewModel
{
    public int BaggageId { get; init; }

    public int TicketId { get; init; }

    public int BookingId { get; init; }

    public string PassengerName { get; init; } = "";

    public string FlightNumber { get; init; } = "";

    public string RouteLabel { get; init; } = "";

    public DateTime? DepartureTime { get; init; }

    public string TicketStatus { get; init; } = "";

    public decimal Weight { get; init; }

    public decimal Price { get; init; }

    public bool IsPaid { get; init; }

    public string PaymentStatus { get; init; } = "";
}

public sealed class UserBaggagePageViewModel
{
    public BaggageRegisterInputModel Form { get; init; } = new();

    public IReadOnlyList<BaggageTicketOptionViewModel> AvailableTickets { get; init; } = [];

    public IReadOnlyList<UserBaggageItemViewModel> RegisteredBaggages { get; init; } = [];

    public int TotalOwnedTickets { get; init; }

    public int EligibleTicketCount { get; init; }

    public decimal TotalRegisteredWeight { get; init; }

    public decimal TotalRegisteredPrice { get; init; }
}

public sealed class AdminBaggageItemViewModel
{
    public int BaggageId { get; init; }

    public int TicketId { get; init; }

    public int BookingId { get; init; }

    public int UserId { get; init; }

    public string CustomerName { get; init; } = "";

    public string Username { get; init; } = "";

    public string PassengerName { get; init; } = "";

    public string FlightNumber { get; init; } = "";

    public string RouteLabel { get; init; } = "";

    public DateTime? DepartureTime { get; init; }

    public string TicketStatus { get; init; } = "";

    public decimal Weight { get; init; }

    public decimal Price { get; init; }

    public bool IsPaid { get; init; }

    public string PaymentStatus { get; init; } = "";
}

public sealed class AdminBaggagePageViewModel
{
    public BaggageRegisterInputModel CreateForm { get; init; } = new();

    public BaggageUpdateWeightModel UpdateForm { get; init; } = new();

    public IReadOnlyList<BaggageTicketOptionViewModel> AvailableTickets { get; init; } = [];

    public IReadOnlyList<AdminBaggageItemViewModel> Baggages { get; init; } = [];

    public int TotalBaggages { get; init; }

    public int TotalCustomers { get; init; }

    public decimal TotalWeight { get; init; }

    public decimal TotalRevenue { get; init; }
}
