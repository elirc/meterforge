using System.Collections.Concurrent;
using MeterForge.Application.Usage;
using MeterForge.Domain.Usage;

namespace MeterForge.Infrastructure.Usage;

public sealed class InMemoryUsageEventStore : IUsageEventStore
{
    private readonly ConcurrentDictionary<(string TenantId, string ExternalEventId), UsageEvent> _events = new();

    public Task<UsageEvent?> FindByExternalIdAsync(string tenantId, string externalEventId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.TryGetValue((tenantId, externalEventId), out var usageEvent);
        return Task.FromResult(usageEvent);
    }

    public Task AddAsync(UsageEvent usageEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.TryAdd((usageEvent.TenantId, usageEvent.ExternalEventId), usageEvent);
        return Task.CompletedTask;
    }
}
