using MeterForge.Domain.Common;
using MeterForge.Domain.Invoicing;

namespace MeterForge.Application.Persistence;

public sealed record InvoiceLineSnapshot(Guid Id, string Description, decimal Amount, string Currency, Guid? PriceVersionId, string SourceReference);

public sealed record InvoiceSnapshot(
    Guid Id, string TenantId, Guid SubscriptionId, string Currency, DateOnly PeriodStart, DateOnly PeriodEnd,
    DateTimeOffset FinalizedAt, IReadOnlyCollection<InvoiceLineSnapshot> Lines)
{
    public static InvoiceSnapshot From(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.Finalized || invoice.FinalizedAt is null)
            throw new BusinessRuleException("invoice.snapshot_requires_final", "Only finalized invoices can be persisted as immutable snapshots.");

        return new InvoiceSnapshot(invoice.Id, invoice.TenantId, invoice.SubscriptionId, invoice.Currency,
            invoice.PeriodStart, invoice.PeriodEnd, invoice.FinalizedAt.Value,
            invoice.Lines.Select(line => new InvoiceLineSnapshot(line.Id, line.Description, line.Amount.Amount,
                line.Amount.Currency, line.PriceVersionId, line.SourceReference)).ToArray());
    }
}

public interface IInvoiceSnapshotStore
{
    Task SaveFinalizedAsync(InvoiceSnapshot snapshot, CancellationToken cancellationToken);
}
