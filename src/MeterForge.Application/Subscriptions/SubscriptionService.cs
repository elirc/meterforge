using MeterForge.Domain.Subscriptions;

namespace MeterForge.Application.Subscriptions;

public interface ISubscriptionRepository
{
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken);
    Task<Subscription?> GetAsync(string tenantId, Guid subscriptionId, CancellationToken cancellationToken);
    Task<Subscription?> MutateAsync(
        string tenantId, Guid subscriptionId, int expectedVersion, Action<Subscription> mutation, CancellationToken cancellationToken);
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

public sealed class SubscriptionAmendmentService(ISubscriptionRepository repository)
{
    public async Task<Subscription?> ChangeProductAsync(
        string tenantId, Guid subscriptionId, Guid productId, int expectedVersion, CancellationToken cancellationToken)
    {
        return await repository.MutateAsync(
            tenantId, subscriptionId, expectedVersion, subscription => subscription.ChangeProduct(productId), cancellationToken)
            .ConfigureAwait(false);
    }
}
