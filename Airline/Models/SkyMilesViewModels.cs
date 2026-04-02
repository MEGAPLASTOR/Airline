using System;
using System.ComponentModel.DataAnnotations;

namespace Airline.Models;

public class BookingSuccessViewModel
{
    public int BookingId { get; set; }

    public int SkyMilesRedeemed { get; set; }

    public int SkyMilesDiscountPercent { get; set; }

    public int CurrentSkyMiles { get; set; }

    public decimal FinalAmount { get; set; }

    public string? PromoCode { get; set; }

    public bool HasSkyMilesDiscount => SkyMilesRedeemed > 0;
}

public class SkyMilesShopViewModel
{
    public int CurrentSkyMiles { get; set; }

    public List<SkyMilesShopItemViewModel> ShopItems { get; set; } = new();

    public List<OwnedSkyMilesPromotionViewModel> OwnedPromotions { get; set; } = new();
}

public class SkyMilesShopItemViewModel
{
    public int PromoId { get; set; }

    public string PromoCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int DiscountPercent { get; set; }

    public int SkyMilesCost { get; set; }

    public string ValidityText { get; set; } = string.Empty;

    public bool AlreadyOwned { get; set; }

    public bool CanAfford { get; set; }
}

public class OwnedSkyMilesPromotionViewModel
{
    public int UserPromotionId { get; set; }

    public string PromoCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int DiscountPercent { get; set; }

    public int SkyMilesSpent { get; set; }

    public DateTime PurchasedAt { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsRedeemed { get; set; }

    public DateTime? RedeemedAt { get; set; }

    public bool IsExpired { get; set; }

    public string StatusText =>
        IsRedeemed ? "Used" :
        IsExpired ? "Expired" :
        "Ready to use";
}

public class ManageSkyMilesViewModel
{
    public SkyMilesPromotionFormModel Form { get; set; } = new();

    public List<AdminSkyMilesPromotionRowViewModel> Promotions { get; set; } = new();
}

public class AdminSkyMilesPromotionRowViewModel
{
    public int PromoId { get; set; }

    public string PromoCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int DiscountPercent { get; set; }

    public int SkyMilesCost { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int TotalPurchased { get; set; }

    public int TotalAvailable { get; set; }

    public int TotalRedeemed { get; set; }
}

public class SkyMilesPromotionFormModel
{
    public int? PromoId { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    public string PromoCode { get; set; } = string.Empty;

    [Range(1, 100)]
    public int DiscountPercent { get; set; }

    [Range(1, int.MaxValue)]
    public int SkyMilesCost { get; set; }

    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
}
