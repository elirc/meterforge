using MeterForge.Contracts.V1.Reconciliation;
using MeterForge.Domain.Common;
using MeterForge.Domain.Reconciliation;

namespace MeterForge.Api.Reconciliation;

public static class ReconciliationEndpoints
{
    public static IEndpointRouteBuilder MapReconciliationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/tenants/{tenantId}/reconciliation/payments", (string tenantId, ReconcilePaymentRequest request) =>
        {
            var result = PaymentReconciler.Compare(new Money(request.InternalAmount, request.Currency), new Money(request.ProviderAmount, request.Currency));
            return Results.Ok(new ReconciliationResponse(result.Matches, result.Difference.Amount, result.Difference.Currency, result.Reason));
        }).WithTags("Reconciliation");
        return endpoints;
    }
}
