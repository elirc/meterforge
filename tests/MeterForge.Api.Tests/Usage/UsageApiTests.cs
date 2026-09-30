using System.Net;
using System.Net.Http.Json;
using MeterForge.Contracts.V1.Usage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MeterForge.Api.Tests.Usage;

public sealed class UsageApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public UsageApiTests(WebApplicationFactory<Program> factory) =>
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing")).CreateClient();

    [Fact]
    public async Task CreateReadReplayConflict_PreservesTheOriginalResource()
    {
        var tenant = $"tenant-{Guid.NewGuid():N}";
        var url = $"/api/v1/tenants/{tenant}/usage-events";
        var request = new IngestUsageRequest(Guid.NewGuid(), "api_calls", 10, DateTimeOffset.UtcNow, "event-1");
        var created = await _client.PostAsJsonAsync(url, request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var original = await created.Content.ReadFromJsonAsync<UsageEventResponse>();
        Assert.NotNull(original);
        Assert.NotNull(created.Headers.Location);
        var read = await _client.GetFromJsonAsync<UsageEventResponse>(created.Headers.Location);
        Assert.Equal(original, read);

        var replay = await _client.PostAsJsonAsync(url, request);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var replayed = await replay.Content.ReadFromJsonAsync<UsageEventResponse>();
        Assert.NotNull(replayed);
        Assert.True(replayed.IsReplay);
        Assert.Equal(original.Id, replayed.Id);

        var conflict = await _client.PostAsJsonAsync(url, request with { Quantity = 11 });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        var problem = await conflict.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("usage.idempotency_conflict", problem?["code"].ToString());
        Assert.True(conflict.Headers.Contains("X-Trace-Id"));
        Assert.Equal(problem?["traceId"].ToString(), conflict.Headers.GetValues("X-Trace-Id").Single());
        Assert.Equal(original, await _client.GetFromJsonAsync<UsageEventResponse>(created.Headers.Location));
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/v1/tenants/other-{tenant}/usage-events/{original.Id}")).StatusCode);
    }

    [Fact]
    public async Task InvalidNewUsage_ReturnsValidationProblem()
    {
        var request = new IngestUsageRequest(Guid.NewGuid(), "requests", 0, DateTimeOffset.UtcNow, "invalid");
        var response = await _client.PostAsJsonAsync($"/api/v1/tenants/{Guid.NewGuid()}/usage-events", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
