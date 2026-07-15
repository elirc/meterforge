using MeterForge.Contracts.V1.Rating;
using MeterForge.Domain.Common;
using MeterForge.Domain.Rating;

namespace MeterForge.Api.Rating;

public static class RatingEndpoints
{
    public static IEndpointRouteBuilder MapRatingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/tenants/{tenantId}/rating/tiered", (string tenantId, RateUsageRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new BusinessRuleException("tenant.required", "Tenant is required.");
            var result = TieredRater.Rate(request.Quantity,
                request.Tiers.Select(tier => new UsageTier(tier.UpTo, new Money(tier.UnitAmount, tier.Currency))).ToArray());
            return Results.Ok(new RatingResponse(result.Quantity, result.Total.Currency, result.Total.Amount,
                result.Tiers.Select(tier => new RatedTierResponse(tier.Units, tier.UnitPrice.Amount, tier.Charge.Amount)).ToArray()));
        }).WithTags("Rating");
        return endpoints;
    }
}
