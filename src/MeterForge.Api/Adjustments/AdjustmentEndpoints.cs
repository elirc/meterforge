using MeterForge.Contracts.V1.Adjustments;
using MeterForge.Domain.Adjustments;
using MeterForge.Domain.Common;

namespace MeterForge.Api.Adjustments;

public static class AdjustmentEndpoints
{
    public static IEndpointRouteBuilder MapAdjustmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/tenants/{tenantId}/discounts/preview", (string tenantId, DiscountPreviewRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new BusinessRuleException("tenant.required", "Tenant is required.");
            var original = new Money(request.Amount, request.Currency);
            var discount = new PercentageDiscount(request.Code, request.Percent);
            var reduction = discount.Apply(original);
            return Results.Ok(new DiscountPreviewResponse(discount.Code, original.Amount, reduction.Amount,
                original.Amount - reduction.Amount, original.Currency));
        }).WithTags("Adjustments");
        return endpoints;
    }
}
