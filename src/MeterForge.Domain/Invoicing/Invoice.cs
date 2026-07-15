using MeterForge.Domain.Common;

namespace MeterForge.Domain.Invoicing;

public enum InvoiceStatus { Draft, Finalized }

public sealed record InvoiceLine(Guid Id, string Description, Money Amount, Guid? PriceVersionId, string SourceReference);

public sealed class Invoice
{
    private readonly List<InvoiceLine> _lines = [];

    public Invoice(Guid id, string tenantId, Guid subscriptionId, string currency, DateOnly periodStart, DateOnly periodEnd)
    {
        if (id == Guid.Empty || subscriptionId == Guid.Empty) throw new BusinessRuleException("invoice.identity_required", "Invoice and subscription IDs are required.");
        if (string.IsNullOrWhiteSpace(tenantId)) throw new BusinessRuleException("tenant.required", "Tenant is required.");
        if (periodEnd <= periodStart) throw new BusinessRuleException("invoice.period_invalid", "Invoice period end must follow its start.");
        Currency = new Money(0, currency).Currency;
        Id = id;
        TenantId = tenantId;
        SubscriptionId = subscriptionId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public Guid SubscriptionId { get; }
    public string Currency { get; }
    public DateOnly PeriodStart { get; }
    public DateOnly PeriodEnd { get; }
    public InvoiceStatus Status { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();
    public Money Total => _lines.Aggregate(new Money(0, Currency), (total, line) => total + line.Amount);

    public void AddLine(string description, Money amount, Guid? priceVersionId, string sourceReference)
    {
        if (amount.Currency != Currency) throw new BusinessRuleException("invoice.currency_mismatch", "Line currency must match the invoice.");
        if (amount.Amount < 0) throw new BusinessRuleException("invoice.line_negative", "Invoice lines cannot be negative.");
        if (string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(sourceReference))
            throw new BusinessRuleException("invoice.line_evidence_required", "Description and source evidence are required.");
        _lines.Add(new InvoiceLine(Guid.NewGuid(), description.Trim(), amount, priceVersionId, sourceReference.Trim()));
    }

    public void Finalize(DateTimeOffset finalizedAt)
    {
        if (Status != InvoiceStatus.Draft) throw new BusinessRuleException("invoice.finalize_invalid", "Only a draft invoice can be finalized.");
        if (_lines.Count == 0) throw new BusinessRuleException("invoice.empty", "An invoice needs at least one line.");
        Status = InvoiceStatus.Finalized;
        FinalizedAt = finalizedAt.ToUniversalTime();
    }
}
