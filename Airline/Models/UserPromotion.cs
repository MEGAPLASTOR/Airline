using System;

namespace Airline.Models;

public partial class UserPromotion
{
    public int UserPromotionId { get; set; }

    public int UserId { get; set; }

    public int PromoId { get; set; }

    public DateTime PurchasedAt { get; set; }

    public int SkyMilesSpent { get; set; }

    public bool IsRedeemed { get; set; }

    public DateTime? RedeemedAt { get; set; }

    public int? RedeemedBookingId { get; set; }

    public virtual Promotion Promo { get; set; } = null!;

    public virtual Booking? RedeemedBooking { get; set; }

    public virtual User User { get; set; } = null!;
}
