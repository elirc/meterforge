using MeterForge.Application.Subscriptions;
using MeterForge.Contracts.V1.Subscriptions;
using MeterForge.Domain.Subscriptions;

namespace MeterForge.Api.Subscriptions;

public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/tenants/{tenantId}/subscriptions").WithTags("Subscriptions");
        group.MapPost("/", async (string tenantId, StartSubscriptionRequest request, SubscriptionService service, CancellationToken cancellationToken) =>
        {
            var subscription = await service.StartAsync(tenantId, request.ProductId, request.StartsOn, request.TrialEndsOn, cancellationToken);
            return Results.Created($"/api/v1/tenants/{tenantId}/subscriptions/{subscription.Id}", Map(subscription));
        });
        group.MapGet("/{id:guid}", async (string tenantId, Guid id, SubscriptionService service, CancellationToken cancellationToken) =>
        {
            var subscription = await service.GetAsync(tenantId, id, cancellationToken);
            return subscription is null ? Results.NotFound() : Results.Ok(Map(subscription));
        });
        group.MapPut("/{id:guid}/product", async (
            string tenantId, Guid id, AmendSubscriptionProductRequest request, HttpRequest httpRequest,
            SubscriptionAmendmentService service, CancellationToken cancellationToken) =>
        {
            if (!int.TryParse(httpRequest.Headers.IfMatch.ToString().Trim('"'), out var expectedVersion))
                return Results.BadRequest(new { code = "subscription.if_match_required" });
            var subscription = await service.ChangeProductAsync(tenantId, id, request.ProductId, expectedVersion, cancellationToken);
            return subscription is null ? Results.NotFound() : Results.Ok(Map(subscription));
        });
        return endpoints;
    }

    private static SubscriptionResponse Map(Subscription subscription) => new(
        subscription.Id, subscription.ProductId, subscription.Status.ToString(), subscription.StartsOn, subscription.TrialEndsOn, subscription.Version);
}
