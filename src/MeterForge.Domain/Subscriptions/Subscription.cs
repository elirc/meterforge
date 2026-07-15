using MeterForge.Domain.Common;

namespace MeterForge.Domain.Subscriptions;

public enum SubscriptionStatus { Trialing, Active, Paused, Cancelled }

public sealed class Subscription
{
    public Subscription(Guid id, string tenantId, Guid productId, DateOnly startsOn, DateOnly? trialEndsOn)
    {
        if (id == Guid.Empty || productId == Guid.Empty) throw new BusinessRuleException("subscription.identity_required", "Subscription and product IDs are required.");
        if (string.IsNullOrWhiteSpace(tenantId)) throw new BusinessRuleException("tenant.required", "Tenant is required.");
        if (trialEndsOn < startsOn) throw new BusinessRuleException("subscription.trial_invalid", "Trial cannot end before the subscription starts.");

        Id = id;
        TenantId = tenantId;
        ProductId = productId;
        StartsOn = startsOn;
        TrialEndsOn = trialEndsOn;
        Status = trialEndsOn is null ? SubscriptionStatus.Active : SubscriptionStatus.Trialing;
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public Guid ProductId { get; }
    public DateOnly StartsOn { get; }
    public DateOnly? TrialEndsOn { get; }
    public SubscriptionStatus Status { get; private set; }
    public int Version { get; private set; } = 1;

    public void Activate()
    {
        if (Status != SubscriptionStatus.Trialing) Reject("subscription.activate_invalid");
        Status = SubscriptionStatus.Active;
        Version++;
    }

    public void Pause()
    {
        if (Status != SubscriptionStatus.Active) Reject("subscription.pause_invalid");
        Status = SubscriptionStatus.Paused;
        Version++;
    }

    public void Resume()
    {
        if (Status != SubscriptionStatus.Paused) Reject("subscription.resume_invalid");
        Status = SubscriptionStatus.Active;
        Version++;
    }

    public void Cancel()
    {
        if (Status == SubscriptionStatus.Cancelled) Reject("subscription.cancel_invalid");
        Status = SubscriptionStatus.Cancelled;
        Version++;
    }

    private static void Reject(string code) => throw new BusinessRuleException(code, "Subscription transition is not valid from the current state.");
}
