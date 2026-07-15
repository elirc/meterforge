using MeterForge.Application.Persistence;
using MeterForge.Domain.Common;
using MeterForge.Domain.Invoicing;

namespace MeterForge.Application.Tests.Persistence;

public sealed class InvoiceSnapshotTests
{
    [Fact]
    public void From_FinalizedInvoice_CopiesImmutableEvidence()
    {
        var invoice = new Invoice(Guid.NewGuid(), "tenant-a", Guid.NewGuid(), "USD", new DateOnly(2026, 6, 1), new DateOnly(2026, 7, 1));
        invoice.AddLine("Plan", new Money(49m, "USD"), Guid.NewGuid(), "subscription:1");
        invoice.Finalize(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));

        var snapshot = InvoiceSnapshot.From(invoice);

        Assert.Equal(invoice.Id, snapshot.Id);
        Assert.Single(snapshot.Lines);
        Assert.Equal("tenant-a", snapshot.TenantId);
    }

    [Fact]
    public void From_DraftInvoice_RejectsPersistence()
    {
        var invoice = new Invoice(Guid.NewGuid(), "tenant-a", Guid.NewGuid(), "USD", new DateOnly(2026, 6, 1), new DateOnly(2026, 7, 1));
        var error = Assert.Throws<BusinessRuleException>(() => InvoiceSnapshot.From(invoice));
        Assert.Equal("invoice.snapshot_requires_final", error.Code);
    }
}
