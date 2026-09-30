using MeterForge.Application.Usage;
using MeterForge.Infrastructure.Usage;
using MeterForge.Domain.Common;

namespace MeterForge.Application.Tests.Usage;

public sealed class UsageIngestionTests
{
    [Fact]
    public async Task Ingest_CompetingServices_ReturnExactlyOneCreationAndOneStoredIdentity()
    {
        var store = new InMemoryUsageEventStore();
        var subscription = Guid.NewGuid();
        var at = DateTimeOffset.UtcNow;
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = Enumerable.Range(0, 64).Select(async _ =>
        {
            await start.Task;
            return await new UsageIngestionService(store).IngestAsync("tenant-race", subscription,
                "requests", 3, at, "same-event", CancellationToken.None);
        }).ToArray();
        start.SetResult();
        var results = await Task.WhenAll(attempts);
        Assert.Single(results, result => !result.IsReplay);
        Assert.Single(results.Select(result => result.Event.Id).Distinct());
        // The winner is what the store kept: a later ingest of the same key must replay it.
        var afterRace = await new UsageIngestionService(store).IngestAsync("tenant-race", subscription,
            "requests", 3, at, "same-event", CancellationToken.None);
        Assert.True(afterRace.IsReplay);
        Assert.Equal(results[0].Event.Id, afterRace.Event.Id);
    }

    [Fact]
    public async Task Ingest_NormalizedReplay_ReturnsOriginalIdentity()
    {
        var service = new UsageIngestionService(new InMemoryUsageEventStore());
        var subscription = Guid.NewGuid();
        var at = DateTimeOffset.UtcNow;
        var first = await service.IngestAsync("tenant-a", subscription, " requests ", 3, at, " event-1 ", default);
        var replay = await service.IngestAsync("tenant-a", subscription, "requests", 3, at.ToOffset(TimeSpan.FromHours(2)), " event-1 ", default);
        Assert.True(replay.IsReplay);
        Assert.Equal(first.Event.Id, replay.Event.Id);
        Assert.Equal("event-1", replay.Event.ExternalEventId);
    }

    [Theory]
    [InlineData("subscription")]
    [InlineData("meter")]
    [InlineData("quantity")]
    [InlineData("time")]
    public async Task Ingest_ChangedPayloadRejectsReplayWithoutChangingOriginal(string field)
    {
        var store = new InMemoryUsageEventStore();
        var service = new UsageIngestionService(store);
        var subscription = Guid.NewGuid();
        var at = DateTimeOffset.UtcNow;
        var first = await service.IngestAsync("tenant-a", subscription, "requests", 3, at, "event-1", default);
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.IngestAsync("tenant-a",
            field == "subscription" ? Guid.NewGuid() : subscription, field == "meter" ? "bytes" : "requests",
            field == "quantity" ? 4 : 3, field == "time" ? at.AddMinutes(1) : at, "event-1", default));
        Assert.Equal("usage.idempotency_conflict", error.Code);
        Assert.Equal(first.Event, await store.FindByIdAsync("tenant-a", first.Event.Id, default));
        Assert.Null(await store.FindByIdAsync("other-tenant", first.Event.Id, default));
    }

    [Fact]
    public async Task Ingest_CancelledRequestDoesNotStoreUsage()
    {
        var store = new InMemoryUsageEventStore();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new UsageIngestionService(store).IngestAsync(
            "tenant-a", Guid.NewGuid(), "requests", 3, DateTimeOffset.UtcNow, "event-cancelled", cancellation.Token));
        // Nothing was stored under that key: the next ingest is a creation, not a replay.
        var afterCancel = await new UsageIngestionService(store).IngestAsync("tenant-a", Guid.NewGuid(),
            "requests", 3, DateTimeOffset.UtcNow, "event-cancelled", CancellationToken.None);
        Assert.False(afterCancel.IsReplay);
    }

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

    [Fact]
    public async Task Ingest_SameExternalIdInDifferentTenants_AcceptsBoth()
    {
        var service = new UsageIngestionService(new InMemoryUsageEventStore());
        var occurredAt = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

        var first = await service.IngestAsync("tenant-a", Guid.NewGuid(), "api_calls", 10, occurredAt, "shared-event", CancellationToken.None);
        var second = await service.IngestAsync("tenant-b", Guid.NewGuid(), "api_calls", 10, occurredAt, "shared-event", CancellationToken.None);

        Assert.False(first.IsReplay);
        Assert.False(second.IsReplay);
        Assert.NotEqual(first.Event.Id, second.Event.Id);
    }
}
