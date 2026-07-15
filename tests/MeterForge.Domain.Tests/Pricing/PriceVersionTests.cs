using MeterForge.Domain.Common;
using MeterForge.Domain.Pricing;

namespace MeterForge.Domain.Tests.Pricing;

public sealed class PriceVersionTests
{
    [Fact]
    public void Select_AdjacentBoundary_ChoosesNewVersionOnly()
    {
        var productId = Guid.NewGuid();
        var boundary = new DateOnly(2026, 7, 1);
        var oldPrice = new PriceVersion(Guid.NewGuid(), productId, new Money(10m, "USD"), new DateOnly(2026, 1, 1), boundary);
        var newPrice = new PriceVersion(Guid.NewGuid(), productId, new Money(12m, "USD"), boundary, null);

        var selected = PriceSelector.Select([oldPrice, newPrice], productId, boundary);

        Assert.Equal(newPrice.Id, selected.Id);
    }

    [Fact]
    public void Select_OverlappingVersions_RejectsAmbiguity()
    {
        var productId = Guid.NewGuid();
        var versions = new[]
        {
            new PriceVersion(Guid.NewGuid(), productId, new Money(10m, "USD"), new DateOnly(2026, 1, 1), null),
            new PriceVersion(Guid.NewGuid(), productId, new Money(12m, "USD"), new DateOnly(2026, 2, 1), null),
        };

        var error = Assert.Throws<BusinessRuleException>(() => PriceSelector.Select(versions, productId, new DateOnly(2026, 3, 1)));

        Assert.Equal("price.overlap", error.Code);
    }
}
