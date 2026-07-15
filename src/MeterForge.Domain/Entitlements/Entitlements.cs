using MeterForge.Domain.Common;

namespace MeterForge.Domain.Entitlements;

public sealed record Entitlement(string Feature, decimal? IncludedQuantity)
{
    public string Feature { get; } = !string.IsNullOrWhiteSpace(Feature)
        ? Feature.Trim().ToLowerInvariant()
        : throw new BusinessRuleException("entitlement.feature_required", "Feature is required.");

    public decimal? IncludedQuantity { get; } = IncludedQuantity is null or >= 0
        ? IncludedQuantity
        : throw new BusinessRuleException("entitlement.quantity_invalid", "Included quantity cannot be negative.");
}

public sealed record EntitlementDecision(bool Allowed, decimal? Remaining, string Reason);

public static class EntitlementEvaluator
{
    public static EntitlementDecision Evaluate(IReadOnlyCollection<Entitlement> entitlements, string feature, decimal consumed, decimal requested)
    {
        if (consumed < 0 || requested <= 0) throw new BusinessRuleException("entitlement.usage_invalid", "Consumed must be non-negative and requested must be positive.");
        var entitlement = entitlements.SingleOrDefault(item => item.Feature == feature.Trim().ToLowerInvariant());
        if (entitlement is null) return new EntitlementDecision(false, 0, "feature_not_entitled");
        if (entitlement.IncludedQuantity is null) return new EntitlementDecision(true, null, "unlimited");
        var remaining = Math.Max(0, entitlement.IncludedQuantity.Value - consumed);
        return requested <= remaining
            ? new EntitlementDecision(true, remaining - requested, "within_limit")
            : new EntitlementDecision(false, remaining, "limit_exceeded");
    }
}
