using MeterForge.Domain.Entitlements;

namespace MeterForge.Domain.Tests.Entitlements;

public sealed class EntitlementTests
{
    [Theory]
    [InlineData(80, 20, true, 0)]
    [InlineData(80, 21, false, 20)]
    public void Evaluate_LimitedFeature_ReturnsRemainingEvidence(decimal consumed, decimal requested, bool allowed, decimal remaining)
    {
        var result = EntitlementEvaluator.Evaluate([new Entitlement("api_calls", 100)], "api_calls", consumed, requested);
        Assert.Equal(allowed, result.Allowed);
        Assert.Equal(remaining, result.Remaining);
    }

    [Fact]
    public void Evaluate_MissingFeature_DeniesWithoutConfusingAuthorization()
    {
        var result = EntitlementEvaluator.Evaluate([], "exports", 0, 1);
        Assert.False(result.Allowed);
        Assert.Equal("feature_not_entitled", result.Reason);
    }
}
