using MeterForge.Domain.Usage;

namespace MeterForge.Application.Usage;

public interface IUsageEventStore
{
    Task<UsageEvent?> FindByExternalIdAsync(string tenantId, string externalEventId, CancellationToken cancellationToken);
    Task AddAsync(UsageEvent usageEvent, CancellationToken cancellationToken);
}

public sealed record UsageIngestionResult(UsageEvent Event, bool IsReplay);

public sealed class UsageIngestionService(IUsageEventStore store)
{
    public async Task<UsageIngestionResult> IngestAsync(
        string tenantId, Guid subscriptionId, string meter, decimal quantity, DateTimeOffset occurredAt, string externalEventId,
        CancellationToken cancellationToken)
    {
        var existing = await store.FindByExternalIdAsync(tenantId, externalEventId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) return new UsageIngestionResult(existing, true);

        var usageEvent = new UsageEvent(Guid.NewGuid(), tenantId, subscriptionId, meter, quantity, occurredAt, externalEventId);
        await store.AddAsync(usageEvent, cancellationToken).ConfigureAwait(false);
        return new UsageIngestionResult(usageEvent, false);
    }
}
