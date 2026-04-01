using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Airline.Models
{
    public class BookingViewModel
    {
        public int ScheduleId { get; set; }
        public int? SeatId { get; set; }
        public int? ClassId { get; set; }
        public string? SeatNumber { get; set; }
        
        [Required(ErrorMessage = "Full name is required.")]
        public string? FullName { get; set; }
        
        [Required(ErrorMessage = "Passenger type is required.")]
        public string? PassengerType { get; set; } = "Adult";
        
        public string? Destination { get; set; }
        public string? Origin { get; set; }
        public DateTime? DepartureTime { get; set; }
        public string? FlightNumber { get; set; }
        public decimal? Price { get; set; }

        [StringLength(50)]
        public string? PromoCode { get; set; }

        public int? AppliedPromotionId { get; set; }

        public int DiscountPercent { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal FinalPrice { get; set; }

        public bool UseSkyMilesPayment { get; set; }

        public int CurrentSkyMiles { get; set; }

        public int RequiredSkyMiles { get; set; }

        public bool CanAffordWithSkyMiles { get; set; }

        public List<Promotion> AvailablePromotions { get; set; } = new();

        public List<Promotion> OwnedSkyMilesPromotions { get; set; } = new();
    }
}
