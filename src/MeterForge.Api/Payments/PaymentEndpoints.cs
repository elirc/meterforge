using MeterForge.Application.Payments;
using MeterForge.Contracts.V1.Payments;
using MeterForge.Domain.Common;

namespace MeterForge.Api.Payments;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/tenants/{tenantId}/payments", async (
            string tenantId, CollectPaymentRequest request, HttpRequest httpRequest, PaymentService service, CancellationToken cancellationToken) =>
        {
            var key = httpRequest.Headers["Idempotency-Key"].ToString();
            if (string.IsNullOrWhiteSpace(key)) return Results.BadRequest(new { code = "payment.idempotency_key_required" });
            var payment = await service.CollectAsync(tenantId, request.InvoiceId, new Money(request.Amount, request.Currency), key, cancellationToken);
            return Results.Ok(new PaymentResponse(payment.Id, payment.InvoiceId, payment.Status.ToString(), payment.AttemptNumber, payment.ProviderReference!));
        }).WithTags("Payments");
        return endpoints;
    }
}
