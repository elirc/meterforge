using MeterForge.Application.Subscriptions;
using MeterForge.Domain.Common;
using MeterForge.Infrastructure.Subscriptions;

namespace MeterForge.Application.Tests.Subscriptions;

public sealed class SubscriptionAmendmentTests
{
    [Fact]
    public async Task ChangeProduct_ExpectedVersion_AdvancesVersion()
    {
        var repository = new InMemorySubscriptionRepository();
        var starter = new SubscriptionService(repository);
        var amendment = new SubscriptionAmendmentService(repository);
        var subscription = await starter.StartAsync("tenant-a", Guid.NewGuid(), new DateOnly(2026, 1, 1), null, CancellationToken.None);

        var changed = await amendment.ChangeProductAsync("tenant-a", subscription.Id, Guid.NewGuid(), 1, CancellationToken.None);

        Assert.Equal(2, changed?.Version);
    }

    [Fact]
    public async Task ChangeProduct_StaleVersion_RejectsConflict()
    {
        var repository = new InMemorySubscriptionRepository();
        var starter = new SubscriptionService(repository);
        var amendment = new SubscriptionAmendmentService(repository);
        var subscription = await starter.StartAsync("tenant-a", Guid.NewGuid(), new DateOnly(2026, 1, 1), null, CancellationToken.None);
        await amendment.ChangeProductAsync("tenant-a", subscription.Id, Guid.NewGuid(), 1, CancellationToken.None);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            amendment.ChangeProductAsync("tenant-a", subscription.Id, Guid.NewGuid(), 1, CancellationToken.None));

        Assert.Equal("subscription.version_conflict", error.Code);
    }
}
