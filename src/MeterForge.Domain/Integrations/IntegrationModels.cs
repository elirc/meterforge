using MeterForge.Domain.Common;

namespace MeterForge.Domain.Integrations;

public sealed record OutboxMessage
{
    public OutboxMessage(Guid id, string tenantId, string eventType, int schemaVersion, string subject, DateTimeOffset occurredAt, string payload)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(eventType) ||
            string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(payload))
            throw new BusinessRuleException("outbox.evidence_required", "Outbox identity, tenant, event type, subject, and payload are required.");
        if (schemaVersion <= 0) throw new BusinessRuleException("outbox.schema_invalid", "Schema version must be positive.");
        Id = id;
        TenantId = tenantId;
        EventType = eventType;
        SchemaVersion = schemaVersion;
        Subject = subject;
        OccurredAt = occurredAt.ToUniversalTime();
        Payload = payload;
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public string EventType { get; }
    public int SchemaVersion { get; }
    public string Subject { get; }
    public DateTimeOffset OccurredAt { get; }
    public string Payload { get; }
}

public enum DeliveryStatus { Pending, Succeeded, Failed }

public sealed class WebhookDelivery(Guid id, Guid messageId, string tenantId, Uri endpoint)
{
    public Guid Id { get; } = id != Guid.Empty ? id : throw new BusinessRuleException("delivery.id_required", "Delivery ID is required.");
    public Guid MessageId { get; } = messageId != Guid.Empty ? messageId : throw new BusinessRuleException("delivery.message_required", "Message ID is required.");
    public string TenantId { get; } = !string.IsNullOrWhiteSpace(tenantId) ? tenantId : throw new BusinessRuleException("tenant.required", "Tenant is required.");
    public Uri Endpoint { get; } = endpoint.IsAbsoluteUri ? endpoint : throw new BusinessRuleException("delivery.endpoint_invalid", "Webhook endpoint must be absolute.");
    public int Attempt { get; private set; } = 1;
    public DeliveryStatus Status { get; private set; }

    public void Fail() => Status = DeliveryStatus.Failed;
    public void Succeed() => Status = DeliveryStatus.Succeeded;

    public void Retry()
    {
        if (Status != DeliveryStatus.Failed) throw new BusinessRuleException("delivery.retry_invalid", "Only a failed delivery can retry.");
        Attempt++;
        Status = DeliveryStatus.Pending;
    }
}
