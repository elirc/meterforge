using MeterForge.Domain.Common;

namespace MeterForge.Domain.Usage;

public sealed record UsageEvent
{
    public UsageEvent(Guid id, string tenantId, Guid subscriptionId, string meter, decimal quantity, DateTimeOffset occurredAt, string externalEventId)
    {
        if (id == Guid.Empty || subscriptionId == Guid.Empty) throw new BusinessRuleException("usage.identity_required", "Usage and subscription IDs are required.");
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(meter) || string.IsNullOrWhiteSpace(externalEventId))
            throw new BusinessRuleException("usage.field_required", "Tenant, meter, and external event ID are required.");
        if (quantity <= 0) throw new BusinessRuleException("usage.quantity_invalid", "Usage quantity must be positive.");

        Id = id;
        TenantId = tenantId;
        SubscriptionId = subscriptionId;
        Meter = meter.Trim();
        Quantity = quantity;
        OccurredAt = occurredAt.ToUniversalTime();
        ExternalEventId = externalEventId.Trim();
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public Guid SubscriptionId { get; }
    public string Meter { get; }
    public decimal Quantity { get; }
    public DateTimeOffset OccurredAt { get; }
    public string ExternalEventId { get; }
}
