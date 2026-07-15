namespace MeterForge.Contracts.V1.Rating;

public sealed record TierInput(decimal? UpTo, decimal UnitAmount, string Currency);
public sealed record RateUsageRequest(decimal Quantity, IReadOnlyCollection<TierInput> Tiers);
public sealed record RatedTierResponse(decimal Units, decimal UnitAmount, decimal Charge);
public sealed record RatingResponse(decimal Quantity, string Currency, decimal Total, IReadOnlyCollection<RatedTierResponse> Tiers);
