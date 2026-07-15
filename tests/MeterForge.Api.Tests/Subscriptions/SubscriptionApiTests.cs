using System.Net;
using System.Net.Http.Json;
using MeterForge.Contracts.V1.Subscriptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MeterForge.Api.Tests.Subscriptions;

public sealed class SubscriptionApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SubscriptionApiTests(WebApplicationFactory<Program> factory) =>
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing")).CreateClient();

    [Fact]
    public async Task StartTrial_ReturnsTrialingSubscription()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/tenants/tenant-a/subscriptions", new StartSubscriptionRequest(
            Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 14)));
        var subscription = await response.Content.ReadFromJsonAsync<SubscriptionResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Trialing", subscription?.Status);
    }
}
