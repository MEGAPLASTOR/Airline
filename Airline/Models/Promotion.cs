using System;
using System.Collections.Generic;

namespace Airline.Models;

public partial class Promotion
{
    public int PromoId { get; set; }

    public string? PromoCode { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? DiscountPercent { get; set; }

    public bool IsSkyMilesExclusive { get; set; }

    public bool OnlyForSkyMilesPayment { get; set; }

    public int SkyMilesCost { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public virtual ICollection<BookingPromotion> BookingPromotions { get; set; } = new List<BookingPromotion>();

    public virtual ICollection<UserPromotion> UserPromotions { get; set; } = new List<UserPromotion>();
}
