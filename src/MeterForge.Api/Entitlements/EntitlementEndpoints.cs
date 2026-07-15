using MeterForge.Contracts.V1.Entitlements;
using MeterForge.Domain.Entitlements;

namespace MeterForge.Api.Entitlements;

public static class EntitlementEndpoints
{
    public static IEndpointRouteBuilder MapEntitlementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/tenants/{tenantId}/entitlements/check", (string tenantId, CheckEntitlementRequest request) =>
        {
            var decision = EntitlementEvaluator.Evaluate(
                request.Entitlements.Select(item => new Entitlement(item.Feature, item.IncludedQuantity)).ToArray(),
                request.Feature, request.Consumed, request.Requested);
            return Results.Ok(new EntitlementDecisionResponse(decision.Allowed, decision.Remaining, decision.Reason));
        }).WithTags("Entitlements");
        return endpoints;
    }
}
