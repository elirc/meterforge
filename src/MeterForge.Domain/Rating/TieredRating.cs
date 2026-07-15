using MeterForge.Domain.Common;

namespace MeterForge.Domain.Rating;

public sealed record UsageTier(decimal? UpTo, Money UnitPrice);

public sealed record RatedTier(decimal Units, Money UnitPrice, Money Charge);

public sealed record RatingResult(decimal Quantity, IReadOnlyCollection<RatedTier> Tiers, Money Total);

public static class TieredRater
{
    public static RatingResult Rate(decimal quantity, IReadOnlyCollection<UsageTier> tiers)
    {
        if (quantity < 0) throw new BusinessRuleException("rating.quantity_invalid", "Quantity cannot be negative.");
        if (tiers.Count == 0) throw new BusinessRuleException("rating.tiers_required", "At least one tier is required.");

        var ordered = tiers.OrderBy(tier => tier.UpTo ?? decimal.MaxValue).ToArray();
        var currency = ordered[0].UnitPrice.Currency;
        if (ordered.Any(tier => tier.UnitPrice.Currency != currency))
            throw new BusinessRuleException("rating.currency_mismatch", "All tiers must use one currency.");

        var rated = new List<RatedTier>();
        decimal previousLimit = 0;
        decimal total = 0;
        foreach (var tier in ordered)
        {
            if (quantity <= previousLimit) break;
            var upper = tier.UpTo ?? quantity;
            var units = Math.Min(quantity, upper) - previousLimit + (previousLimit == 0 ? 0 : 1);
            if (units > 0)
            {
                var amount = decimal.Round(units * tier.UnitPrice.Amount, 2, MidpointRounding.ToEven);
                var charge = new Money(amount, currency);
                rated.Add(new RatedTier(units, tier.UnitPrice, charge));
                total += amount;
            }

            previousLimit = upper;
        }

        return new RatingResult(quantity, rated, new Money(decimal.Round(total, 2, MidpointRounding.ToEven), currency));
    }
}
