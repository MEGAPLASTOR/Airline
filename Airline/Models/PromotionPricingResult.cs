namespace Airline.Models;

public class PromotionPricingResult
{
    public decimal BaseFare { get; set; }

    public decimal TaxAmount { get; set; }

    public int DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public int SkyMilesDiscountPercent { get; set; }

    public int SkyMilesRedeemed { get; set; }

    public decimal SkyMilesDiscountAmount { get; set; }

    public decimal TotalDiscountAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public int? AppliedPromotionId { get; set; }

    public string? AppliedPromotionCode { get; set; }

    public bool HasPromotion => AppliedPromotionId.HasValue;

    public bool HasSkyMilesDiscount => SkyMilesRedeemed > 0;
}
