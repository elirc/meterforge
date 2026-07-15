using MeterForge.Application.Usage;
using MeterForge.Contracts.V1.Usage;

namespace MeterForge.Api.Usage;

public static class UsageEndpoints
{
    public static IEndpointRouteBuilder MapUsageEndpoints(this IEndpointRouteBuilder endpoints)
    {
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
