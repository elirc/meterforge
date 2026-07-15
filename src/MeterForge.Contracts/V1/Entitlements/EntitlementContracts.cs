namespace MeterForge.Contracts.V1.Entitlements;

public sealed record EntitlementInput(string Feature, decimal? IncludedQuantity);
public sealed record CheckEntitlementRequest(string Feature, decimal Consumed, decimal Requested, IReadOnlyCollection<EntitlementInput> Entitlements);
public sealed record EntitlementDecisionResponse(bool Allowed, decimal? Remaining, string Reason);
