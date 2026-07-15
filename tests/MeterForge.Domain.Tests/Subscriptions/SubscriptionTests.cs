using MeterForge.Domain.Common;
using MeterForge.Domain.Subscriptions;

namespace MeterForge.Domain.Tests.Subscriptions;

public sealed class SubscriptionTests
{
    [Fact]
    public void Trial_ActivatePauseResumeCancel_FollowsExplicitStates()
    {
        var subscription = new Subscription(Guid.NewGuid(), "tenant-a", Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 14));

        subscription.Activate();
        subscription.Pause();
        subscription.Resume();
        subscription.Cancel();

        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
        Assert.Equal(5, subscription.Version);
    }

    [Fact]
    public void Cancel_Twice_RejectsTransition()
    {
        var subscription = new Subscription(Guid.NewGuid(), "tenant-a", Guid.NewGuid(), new DateOnly(2026, 1, 1), null);
        subscription.Cancel();

        var error = Assert.Throws<BusinessRuleException>(subscription.Cancel);

        Assert.Equal("subscription.cancel_invalid", error.Code);
    }
}
