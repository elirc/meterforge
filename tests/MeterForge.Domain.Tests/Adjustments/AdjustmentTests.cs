using MeterForge.Domain.Adjustments;
using MeterForge.Domain.Common;

namespace MeterForge.Domain.Tests.Adjustments;

public sealed class AdjustmentTests
{
    [Fact]
    public void Discount_RoundsReductionWithoutChangingOriginal()
    {
        var original = new Money(19.99m, "USD");
        var reduction = new PercentageDiscount("save10", 10).Apply(original);

        Assert.Equal(2m, reduction.Amount);
        Assert.Equal(19.99m, original.Amount);
    }

    [Fact]
    public void Credit_NegativeOrZero_RejectsRule()
    {
        var error = Assert.Throws<BusinessRuleException>(() => new CreditNote(
            Guid.NewGuid(), "tenant-a", Guid.NewGuid(), new Money(0, "USD"), "correction", DateTimeOffset.UtcNow));
        Assert.Equal("credit.amount_invalid", error.Code);
    }
}
