namespace MeterForge.Contracts.V1.Subscriptions;

public sealed record StartSubscriptionRequest(Guid ProductId, DateOnly StartsOn, DateOnly? TrialEndsOn);

public sealed record SubscriptionResponse(Guid Id, Guid ProductId, string Status, DateOnly StartsOn, DateOnly? TrialEndsOn, int Version);
