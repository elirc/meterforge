using System.Collections.Concurrent;
using MeterForge.Application.Usage;
using MeterForge.Domain.Usage;

namespace MeterForge.Infrastructure.Usage;

public sealed class InMemoryUsageEventStore : IUsageEventStore
{
    private readonly ConcurrentDictionary<string, UsageEvent> _events = new(StringComparer.Ordinal);

    public Task<UsageEvent?> FindByExternalIdAsync(string externalEventId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.TryGetValue(externalEventId, out var usageEvent);
        return Task.FromResult(usageEvent);
    }

    public Task AddAsync(UsageEvent usageEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.TryAdd(usageEvent.ExternalEventId, usageEvent);
        return Task.CompletedTask;
    }
}
