using MeterForge.Application.Usage;
using MeterForge.Contracts.V1.Usage;

namespace MeterForge.Api.Usage;

public static class UsageEndpoints
{
    public static IEndpointRouteBuilder MapUsageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/tenants/{tenantId}/usage-events/{id:guid}", async (
            string tenantId, Guid id, IUsageEventStore store, CancellationToken cancellationToken) =>
        {
            var usage = await store.FindByIdAsync(tenantId, id, cancellationToken);
            return usage is null ? Results.NotFound() : Results.Ok(new UsageEventResponse(
                usage.Id, usage.SubscriptionId, usage.Meter, usage.Quantity, usage.OccurredAt, usage.ExternalEventId, false));
        }).WithTags("Usage");
        endpoints.MapPost("/api/v1/tenants/{tenantId}/usage-events", async (
            string tenantId, IngestUsageRequest request, UsageIngestionService service, CancellationToken cancellationToken) =>
        {
            var result = await service.IngestAsync(tenantId, request.SubscriptionId, request.Meter, request.Quantity,
                request.OccurredAt, request.ExternalEventId, cancellationToken);
            var response = new UsageEventResponse(result.Event.Id, result.Event.SubscriptionId, result.Event.Meter,
                result.Event.Quantity, result.Event.OccurredAt, result.Event.ExternalEventId, result.IsReplay);
            return result.IsReplay ? Results.Ok(response) : Results.Created($"/api/v1/tenants/{tenantId}/usage-events/{result.Event.Id}", response);
        }).WithTags("Usage");
        return endpoints;
    }
}
