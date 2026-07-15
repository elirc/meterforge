using MeterForge.Domain.Common;
using MeterForge.Domain.Reconciliation;

namespace MeterForge.Domain.Tests.Reconciliation;

public sealed class ReconciliationTests
{
    [Theory]
    [InlineData(100, 100, true, 0)]
    [InlineData(100, 99, false, -1)]
    public void Compare_ReportsExactDifference(decimal internalAmount, decimal providerAmount, bool matches, decimal difference)
    {
        var result = PaymentReconciler.Compare(new Money(internalAmount, "USD"), new Money(providerAmount, "USD"));
        Assert.Equal(matches, result.Matches);
        Assert.Equal(difference, result.Difference.Amount);
    }

    [Fact]
    public void Refund_AboveOriginalPayment_RejectsRule()
    {
        var error = Assert.Throws<BusinessRuleException>(() => new Refund(Guid.NewGuid(), "tenant-a", Guid.NewGuid(),
            new Money(100, "USD"), new Money(101, "USD"), "customer request", DateTimeOffset.UtcNow));
        Assert.Equal("refund.amount_invalid", error.Code);
    }
}
