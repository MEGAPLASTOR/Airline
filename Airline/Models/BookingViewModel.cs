using System;
using System.ComponentModel.DataAnnotations;

namespace Airline.Models
{
    public class BookingViewModel
    {
        public int ScheduleId { get; set; }
        public int? SeatId { get; set; }
        public int? ClassId { get; set; }
        public string? SeatNumber { get; set; }
        
        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        public string? FullName { get; set; }
        
        [Required(ErrorMessage = "Loại hành khách là bắt buộc")]
        public string? PassengerType { get; set; } = "Người lớn";
        
        public string? Destination { get; set; }
        public string? Origin { get; set; }
        public DateTime? DepartureTime { get; set; }
        public string? FlightNumber { get; set; }
        public decimal? Price { get; set; }
    }
}
