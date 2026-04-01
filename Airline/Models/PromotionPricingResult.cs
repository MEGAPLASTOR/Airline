namespace Airline.Models;

public class PromotionPricingResult
{
    public decimal BaseFare { get; set; }

    public decimal TaxAmount { get; set; }

    public int DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public int RequiredSkyMiles { get; set; }

    public int? AppliedPromotionId { get; set; }

    public string? AppliedPromotionCode { get; set; }

    public bool HasPromotion => AppliedPromotionId.HasValue;
}
