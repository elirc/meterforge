using MeterForge.Contracts.V1.Invoicing;
using MeterForge.Domain.Common;
using MeterForge.Domain.Invoicing;

namespace MeterForge.Api.Invoicing;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/tenants/{tenantId}/invoices/finalize-preview", (string tenantId, FinalizeInvoiceRequest request) =>
        {
            var invoice = new Invoice(Guid.NewGuid(), tenantId, request.SubscriptionId, request.Currency, request.PeriodStart, request.PeriodEnd);
            foreach (var line in request.Lines)
                invoice.AddLine(line.Description, new Money(line.Amount, request.Currency), line.PriceVersionId, line.SourceReference);
            invoice.Finalize(DateTimeOffset.UtcNow);
            return Results.Ok(new FinalizedInvoiceResponse(invoice.Id, invoice.Status.ToString(), invoice.Total.Amount,
                invoice.Currency, invoice.FinalizedAt!.Value, invoice.Lines.Count));
        }).WithTags("Invoicing");
        return endpoints;
    }
}
