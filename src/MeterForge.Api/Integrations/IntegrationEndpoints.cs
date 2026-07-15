using MeterForge.Contracts.V1.Integrations;
using MeterForge.Domain.Integrations;

namespace MeterForge.Api.Integrations;

public static class IntegrationEndpoints
{
    public static IEndpointRouteBuilder MapIntegrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/tenants/{tenantId}/webhooks/retry-preview", (string tenantId, RetryWebhookPreviewRequest request) =>
        {
            var delivery = new WebhookDelivery(Guid.NewGuid(), request.MessageId, tenantId, request.Endpoint);
            delivery.Fail();
            delivery.Retry();
            return Results.Ok(new WebhookDeliveryResponse(delivery.Id, delivery.MessageId, delivery.Attempt, delivery.Status.ToString()));
        }).WithTags("Integrations");
        return endpoints;
    }
}
