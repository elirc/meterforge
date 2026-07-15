namespace MeterForge.Contracts.V1.Integrations;

public sealed record RetryWebhookPreviewRequest(Guid MessageId, Uri Endpoint);
public sealed record WebhookDeliveryResponse(Guid DeliveryId, Guid MessageId, int Attempt, string Status);
