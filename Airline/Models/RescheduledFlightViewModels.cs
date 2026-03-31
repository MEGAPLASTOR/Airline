namespace Airline.Models;

public sealed class RescheduledFlightPageViewModel
{
    public IReadOnlyList<RescheduledFlightItemViewModel> Flights { get; init; } = Array.Empty<RescheduledFlightItemViewModel>();
    public int TotalAffectedFlights { get; init; }
    public int DelayedFlights { get; init; }
    public int CancelledFlights { get; init; }
}

public sealed class RescheduledFlightItemViewModel
{
    public int TicketId { get; init; }
    public int BookingId { get; init; }
    public string PassengerName { get; init; } = string.Empty;
    public string FlightNumber { get; init; } = string.Empty;
    public string Origin { get; init; } = string.Empty;
    public string Destination { get; init; } = string.Empty;
    public DateTime DepartureTime { get; init; }
    public DateTime ArrivalTime { get; init; }
    public string ScheduleStatus { get; init; } = string.Empty;
    public string TicketStatus { get; init; } = string.Empty;
    public string SeatNumber { get; init; } = string.Empty;
    public string FareClass { get; init; } = string.Empty;
    public string GuidanceText { get; init; } = string.Empty;
}
