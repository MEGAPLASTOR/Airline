using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Airline.Models;

public class Seat
{
    [Key]
    public int SeatId { get; set; }

    [Required]
    public int FlightId { get; set; }

    [Required]
    [MaxLength(10)]
    public string SeatNumber { get; set; } = null!;

    [Required]
    public int ClassId { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey("FlightId")]
    public virtual Flight Flight { get; set; } = null!;

    [ForeignKey("ClassId")]
    public virtual TicketClass Class { get; set; } = null!;
}
