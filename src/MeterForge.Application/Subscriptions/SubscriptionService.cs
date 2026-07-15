using MeterForge.Domain.Subscriptions;

namespace MeterForge.Application.Subscriptions;

public interface ISubscriptionRepository
{
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken);
    Task<Subscription?> GetAsync(string tenantId, Guid subscriptionId, CancellationToken cancellationToken);
}

public sealed class SubscriptionService(ISubscriptionRepository repository)
{
    public async Task<Subscription> StartAsync(string tenantId, Guid productId, DateOnly startsOn, DateOnly? trialEndsOn, CancellationToken cancellationToken)
    {
        var subscription = new Subscription(Guid.NewGuid(), tenantId, productId, startsOn, trialEndsOn);
        await repository.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
        return subscription;
    }

    public Task<Subscription?> GetAsync(string tenantId, Guid id, CancellationToken cancellationToken) => repository.GetAsync(tenantId, id, cancellationToken);
}
