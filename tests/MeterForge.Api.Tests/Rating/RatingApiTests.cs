using System.Net;
using System.Net.Http.Json;
using MeterForge.Contracts.V1.Rating;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MeterForge.Api.Tests.Rating;

public sealed class RatingApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RatingApiTests(WebApplicationFactory<Program> factory) =>
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing")).CreateClient();

    [Fact]
    public async Task Rate_ReturnsItemizedTierEvidence()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/tenants/tenant-a/rating/tiered", new RateUsageRequest(50,
            [new TierInput(100, 1m, "USD"), new TierInput(null, 0.5m, "USD")]));
        var rating = await response.Content.ReadFromJsonAsync<RatingResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(50m, rating?.Total);
        Assert.Single(rating?.Tiers ?? []);
    }
}
