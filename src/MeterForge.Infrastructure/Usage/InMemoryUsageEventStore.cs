using System.Collections.Concurrent;
using MeterForge.Application.Usage;
using MeterForge.Domain.Usage;

namespace MeterForge.Infrastructure.Usage;

public sealed class InMemoryUsageEventStore : IUsageEventStore
{
    private readonly ConcurrentDictionary<(string TenantId, string ExternalEventId), UsageEvent> _events = new();

    public Task<UsageEvent?> FindByIdAsync(string tenantId, Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_events.Values.FirstOrDefault(item => item.TenantId == tenantId && item.Id == id));
    }

    public Task<UsageEvent> GetOrAddAsync(UsageEvent candidate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stored = _events.GetOrAdd((candidate.TenantId, candidate.ExternalEventId), candidate);
        return Task.FromResult(stored);
    }
}
