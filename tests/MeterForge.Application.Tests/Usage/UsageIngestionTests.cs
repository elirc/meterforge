using MeterForge.Application.Usage;
using MeterForge.Infrastructure.Usage;

namespace MeterForge.Application.Tests.Usage;

public sealed class UsageIngestionTests
{
    [Fact]
    public async Task Ingest_SameExternalEvent_ReturnsOriginalAsReplay()
    {
        var service = new UsageIngestionService(new InMemoryUsageEventStore());
        var subscriptionId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

        var first = await service.IngestAsync("tenant-a", subscriptionId, "api_calls", 10, occurredAt, "evt-1", CancellationToken.None);
        var replay = await service.IngestAsync("tenant-a", subscriptionId, "api_calls", 10, occurredAt, "evt-1", CancellationToken.None);

        Assert.False(first.IsReplay);
        Assert.True(replay.IsReplay);
        Assert.Equal(first.Event.Id, replay.Event.Id);
    }
}
