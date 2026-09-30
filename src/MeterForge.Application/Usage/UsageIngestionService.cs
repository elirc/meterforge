using MeterForge.Domain.Usage;
using MeterForge.Domain.Common;

namespace MeterForge.Application.Usage;

public interface IUsageEventStore
{
    Task<UsageEvent?> FindByIdAsync(string tenantId, Guid id, CancellationToken cancellationToken);
    // Atomically return the stored winner for the tenant/external-event key.
    Task<UsageEvent> GetOrAddAsync(UsageEvent candidate, CancellationToken cancellationToken);
}

public sealed record UsageIngestionResult(UsageEvent Event, bool IsReplay);

public sealed class UsageIngestionService(IUsageEventStore store)
{
    public async Task<UsageIngestionResult> IngestAsync(
        string tenantId, Guid subscriptionId, string meter, decimal quantity, DateTimeOffset occurredAt, string externalEventId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Validate and normalize before choosing the idempotency key.
        var candidate = new UsageEvent(Guid.NewGuid(), tenantId, subscriptionId, meter, quantity, occurredAt, externalEventId);
        var stored = await store.GetOrAddAsync(candidate, cancellationToken).ConfigureAwait(false);
        if (stored.SubscriptionId != candidate.SubscriptionId || stored.Meter != candidate.Meter ||
            stored.Quantity != candidate.Quantity || stored.OccurredAt != candidate.OccurredAt)
        {
            throw new BusinessRuleException("usage.idempotency_conflict", "This external event ID already identifies different usage. Use a new ID for a new event.");
        }
        return new UsageIngestionResult(stored, stored.Id != candidate.Id);
    }
}
