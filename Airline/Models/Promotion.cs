using System;
using System.Collections.Generic;

namespace Airline.Models;

public partial class Promotion
{
    public int PromoId { get; set; }

    public string? PromoCode { get; set; }

    public int? DiscountPercent { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public virtual ICollection<BookingPromotion> BookingPromotions { get; set; } = new List<BookingPromotion>();
}
