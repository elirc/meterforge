using MeterForge.Domain.Common;

namespace MeterForge.Domain.Reconciliation;

public sealed record ReconciliationResult(bool Matches, Money Difference, string Reason);

public static class PaymentReconciler
{
    public static ReconciliationResult Compare(Money internalAmount, Money providerAmount)
    {
        if (internalAmount.Currency != providerAmount.Currency)
            throw new BusinessRuleException("reconciliation.currency_mismatch", "Provider and internal amounts must share a currency.");
        var difference = new Money(providerAmount.Amount - internalAmount.Amount, internalAmount.Currency);
        return difference.Amount == 0
            ? new ReconciliationResult(true, difference, "matched")
            : new ReconciliationResult(false, difference, "amount_mismatch");
    }
}

public sealed record Refund
{
    public Refund(Guid id, string tenantId, Guid paymentId, Money originalPayment, Money amount, string reason, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty || paymentId == Guid.Empty) throw new BusinessRuleException("refund.identity_required", "Refund and payment IDs are required.");
        if (amount.Currency != originalPayment.Currency) throw new BusinessRuleException("refund.currency_mismatch", "Refund currency must match payment.");
        if (amount.Amount <= 0 || amount.Amount > originalPayment.Amount) throw new BusinessRuleException("refund.amount_invalid", "Refund must be positive and cannot exceed payment.");
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(reason)) throw new BusinessRuleException("refund.evidence_required", "Tenant and reason are required.");
        Id = id;
        TenantId = tenantId;
        PaymentId = paymentId;
        Amount = amount;
        Reason = reason.Trim();
        CreatedAt = createdAt.ToUniversalTime();
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public Guid PaymentId { get; }
    public Money Amount { get; }
    public string Reason { get; }
    public DateTimeOffset CreatedAt { get; }
}

public sealed record PaymentDispute(Guid Id, string TenantId, Guid PaymentId, string ProviderCaseId, Money DisputedAmount, DateTimeOffset OpenedAt);
