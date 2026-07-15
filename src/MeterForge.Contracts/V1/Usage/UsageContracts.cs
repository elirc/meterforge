namespace MeterForge.Contracts.V1.Usage;

public sealed record IngestUsageRequest(Guid SubscriptionId, string Meter, decimal Quantity, DateTimeOffset OccurredAt, string ExternalEventId);
public sealed record UsageEventResponse(Guid Id, Guid SubscriptionId, string Meter, decimal Quantity, DateTimeOffset OccurredAt, string ExternalEventId, bool IsReplay);
