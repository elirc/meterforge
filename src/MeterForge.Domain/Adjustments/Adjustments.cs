using MeterForge.Domain.Common;

namespace MeterForge.Domain.Adjustments;

public sealed record PercentageDiscount
{
    public PercentageDiscount(string code, decimal percent)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new BusinessRuleException("discount.code_required", "Discount code is required.");
        if (percent <= 0 || percent > 100) throw new BusinessRuleException("discount.percent_invalid", "Discount percent must be above zero and at most 100.");
        Code = code.Trim().ToUpperInvariant();
        Percent = percent;
    }

    public string Code { get; }
    public decimal Percent { get; }

    public Money Apply(Money amount) => new(decimal.Round(amount.Amount * Percent / 100m, 2, MidpointRounding.ToEven), amount.Currency);
}

public sealed record CreditNote
{
    public CreditNote(Guid id, string tenantId, Guid invoiceId, Money amount, string reason, DateTimeOffset issuedAt)
    {
        if (id == Guid.Empty || invoiceId == Guid.Empty) throw new BusinessRuleException("credit.identity_required", "Credit and invoice IDs are required.");
        if (amount.Amount <= 0) throw new BusinessRuleException("credit.amount_invalid", "Credit amount must be positive.");
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(reason)) throw new BusinessRuleException("credit.evidence_required", "Tenant and reason are required.");
        Id = id;
        TenantId = tenantId;
        InvoiceId = invoiceId;
        Amount = amount;
        Reason = reason.Trim();
        IssuedAt = issuedAt.ToUniversalTime();
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public Guid InvoiceId { get; }
    public Money Amount { get; }
    public string Reason { get; }
    public DateTimeOffset IssuedAt { get; }
}
