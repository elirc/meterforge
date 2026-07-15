using MeterForge.Domain.Common;

namespace MeterForge.Domain.Payments;

public enum PaymentStatus { Pending, Succeeded, Failed }

public sealed class PaymentAttempt
{
    public PaymentAttempt(Guid id, string tenantId, Guid invoiceId, Money amount, string providerIdempotencyKey)
    {
        if (id == Guid.Empty || invoiceId == Guid.Empty) throw new BusinessRuleException("payment.identity_required", "Payment and invoice IDs are required.");
        if (amount.Amount <= 0) throw new BusinessRuleException("payment.amount_invalid", "Payment amount must be positive.");
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(providerIdempotencyKey))
            throw new BusinessRuleException("payment.evidence_required", "Tenant and provider idempotency key are required.");
        Id = id;
        TenantId = tenantId;
        InvoiceId = invoiceId;
        Amount = amount;
        ProviderIdempotencyKey = providerIdempotencyKey;
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public Guid InvoiceId { get; }
    public Money Amount { get; }
    public string ProviderIdempotencyKey { get; private set; }
    public PaymentStatus Status { get; private set; }
    public int AttemptNumber { get; private set; } = 1;
    public string? ProviderReference { get; private set; }

    public void Succeed(string providerReference)
    {
        if (Status != PaymentStatus.Pending || string.IsNullOrWhiteSpace(providerReference))
            throw new BusinessRuleException("payment.succeed_invalid", "Only a pending payment with provider evidence can succeed.");
        ProviderReference = providerReference;
        Status = PaymentStatus.Succeeded;
    }

    public void Fail() => Status = PaymentStatus.Failed;

    public void Retry()
    {
        if (Status != PaymentStatus.Failed) throw new BusinessRuleException("payment.retry_invalid", "Only a failed payment can retry.");
        AttemptNumber++;
        Status = PaymentStatus.Pending;
    }
}
