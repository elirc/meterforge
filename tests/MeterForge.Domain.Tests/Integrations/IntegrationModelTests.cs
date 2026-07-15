using MeterForge.Domain.Integrations;

namespace MeterForge.Domain.Tests.Integrations;

public sealed class IntegrationModelTests
{
    [Fact]
    public void Retry_PreservesDeliveryAndMessageIdentity()
    {
        var delivery = new WebhookDelivery(Guid.NewGuid(), Guid.NewGuid(), "tenant-a", new Uri("https://example.test/hooks"));
        var deliveryId = delivery.Id;
        var messageId = delivery.MessageId;
        delivery.Fail();

        delivery.Retry();

        Assert.Equal(deliveryId, delivery.Id);
        Assert.Equal(messageId, delivery.MessageId);
        Assert.Equal(2, delivery.Attempt);
    }

    [Fact]
    public void OutboxMessage_NormalizesOperationalTimeToUtc()
    {
        var message = new OutboxMessage(Guid.NewGuid(), "tenant-a", "invoice.finalized", 1, "invoice/1",
            new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.FromHours(2)), "{}");
        Assert.Equal(TimeSpan.Zero, message.OccurredAt.Offset);
    }
}
