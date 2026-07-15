using MeterForge.Domain.Common;
using MeterForge.Domain.Usage;

namespace MeterForge.Domain.Tests.Usage;

public sealed class UsageEventTests
{
    [Fact]
    public void Create_NonPositiveQuantity_RejectsRule()
    {
        var error = Assert.Throws<BusinessRuleException>(() => new UsageEvent(
            Guid.NewGuid(), "tenant-a", Guid.NewGuid(), "api_calls", 0, DateTimeOffset.UtcNow, "evt-1"));

        Assert.Equal("usage.quantity_invalid", error.Code);
    }
}
