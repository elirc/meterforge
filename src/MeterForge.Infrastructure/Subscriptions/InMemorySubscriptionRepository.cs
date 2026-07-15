using System.Collections.Concurrent;
using MeterForge.Application.Subscriptions;
using MeterForge.Domain.Subscriptions;

namespace MeterForge.Infrastructure.Subscriptions;

public sealed class InMemorySubscriptionRepository : ISubscriptionRepository
{
    private readonly ConcurrentDictionary<(string Tenant, Guid Id), Subscription> _items = new();

    public Task AddAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items[(subscription.TenantId, subscription.Id)] = subscription;
        return Task.CompletedTask;
    }

    public Task<Subscription?> GetAsync(string tenantId, Guid subscriptionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items.TryGetValue((tenantId, subscriptionId), out var subscription);
        return Task.FromResult(subscription);
    }
}
