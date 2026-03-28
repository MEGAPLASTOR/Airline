using System;
using System.ComponentModel.DataAnnotations;

namespace Airline.Models
{
    public class BookingViewModel
    {
        public int ScheduleId { get; set; }
        public string SeatNumber { get; set; }
        
        [Required(ErrorMessage = "Full Name is required")]
        public string FullName { get; set; }
        
        [Required(ErrorMessage = "Passenger Type is required")]
        public string PassengerType { get; set; } = "Adult";
        
        public string Destination { get; set; }
        public string Origin { get; set; }
        public DateTime DepartureTime { get; set; }
        public string FlightNumber { get; set; }
        public decimal Price { get; set; }
    }
}
