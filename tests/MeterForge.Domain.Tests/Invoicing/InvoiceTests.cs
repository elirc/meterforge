using MeterForge.Domain.Common;
using MeterForge.Domain.Invoicing;

namespace MeterForge.Domain.Tests.Invoicing;

public sealed class InvoiceTests
{
    [Fact]
    public void Finalize_PreservesLineEvidenceAndTotal()
    {
        var invoice = Create();
        var priceVersionId = Guid.NewGuid();
        invoice.AddLine("Base plan", new Money(49m, "USD"), priceVersionId, "subscription:1");
        invoice.AddLine("Usage", new Money(12.50m, "USD"), priceVersionId, "meter:api_calls");

        invoice.Finalize(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(61.50m, invoice.Total.Amount);
        Assert.Equal(InvoiceStatus.Finalized, invoice.Status);
        Assert.All(invoice.Lines, line => Assert.NotEmpty(line.SourceReference));
    }

    [Fact]
    public void Finalize_EmptyInvoice_RejectsRule()
    {
        var error = Assert.Throws<BusinessRuleException>(() => Create().Finalize(DateTimeOffset.UtcNow));
        Assert.Equal("invoice.empty", error.Code);
    }

    private static Invoice Create() => new(Guid.NewGuid(), "tenant-a", Guid.NewGuid(), "USD", new DateOnly(2026, 6, 1), new DateOnly(2026, 7, 1));
}
