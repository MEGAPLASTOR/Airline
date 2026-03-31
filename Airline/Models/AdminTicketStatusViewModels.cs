namespace Airline.Models;

public sealed class AdminTicketStatusesViewModel
{
    public IReadOnlyList<AdminTicketStatusRowViewModel> Tickets { get; init; } = Array.Empty<AdminTicketStatusRowViewModel>();
    public string SearchTerm { get; init; } = string.Empty;
    public string SelectedStatus { get; init; } = string.Empty;
    public int TotalTickets { get; init; }
    public int PaidTickets { get; init; }
    public int BookedTickets { get; init; }
    public int CancelledTickets { get; init; }
    public IReadOnlyList<string> AvailableStatuses { get; init; } = Array.Empty<string>();
}

public sealed class AdminTicketStatusRowViewModel
{
    public int TicketId { get; init; }
    public int BookingId { get; init; }
    public string PassengerName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string FlightNumber { get; init; } = string.Empty;
    public string RouteSummary { get; init; } = string.Empty;
    public DateTime? DepartureTime { get; init; }
    public DateTime? BookingDate { get; init; }
    public string SeatNumber { get; init; } = string.Empty;
    public string FareClass { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsLocked { get; init; }
}
