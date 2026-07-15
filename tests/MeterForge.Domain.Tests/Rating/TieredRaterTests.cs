using MeterForge.Domain.Common;
using MeterForge.Domain.Rating;

namespace MeterForge.Domain.Tests.Rating;

public sealed class TieredRaterTests
{
    [Fact]
    public void Rate_QuantityWithinFirstTier_ChargesUnitsAtFirstPrice()
    {
        var result = TieredRater.Rate(50, [
            new UsageTier(100, new Money(1m, "USD")),
            new UsageTier(null, new Money(0.50m, "USD")),
        ]);

        Assert.Equal(50m, result.Total.Amount);
        Assert.Single(result.Tiers);
    }

    [Fact]
    public void Rate_MixedCurrencies_RejectsRule()
    {
        var error = Assert.Throws<BusinessRuleException>(() => TieredRater.Rate(10, [
            new UsageTier(100, new Money(1m, "USD")),
            new UsageTier(null, new Money(1m, "EUR")),
        ]));

        Assert.Equal("rating.currency_mismatch", error.Code);
    }
}
