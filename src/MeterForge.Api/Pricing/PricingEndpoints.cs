using MeterForge.Contracts.V1.Pricing;
using MeterForge.Domain.Common;
using MeterForge.Domain.Pricing;

namespace MeterForge.Api.Pricing;

public static class PricingEndpoints
{
    public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/tenants/{tenantId}/pricing/select", (string tenantId, SelectPriceRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new BusinessRuleException("tenant.required", "Tenant is required.");
            var versions = request.Versions.Select(input => new PriceVersion(
                input.Id, request.ProductId, new Money(input.Amount, input.Currency), input.ValidFrom, input.ValidTo)).ToArray();
            var selected = PriceSelector.Select(versions, request.ProductId, request.AsOf);
            return Results.Ok(new SelectedPriceResponse(selected.Id, selected.UnitPrice.Amount, selected.UnitPrice.Currency, request.AsOf));
        }).WithTags("Pricing");
        return endpoints;
    }
}
