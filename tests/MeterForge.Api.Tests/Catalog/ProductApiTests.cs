using System.Net;
using System.Net.Http.Json;
using MeterForge.Contracts.V1.Catalog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MeterForge.Api.Tests.Catalog;

public sealed class ProductApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductApiTests(WebApplicationFactory<Program> factory) =>
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing")).CreateClient();

    [Fact]
    public async Task PostThenGet_ReturnsVersionedProductContract()
    {
        var tenant = $"tenant-{Guid.NewGuid():N}";
        var create = await _client.PostAsJsonAsync($"/api/v1/tenants/{tenant}/products", new CreateProductRequest("PRO", "Pro", 49m, "USD"));
        var product = await create.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.NotNull(product);

        var get = await _client.GetAsync($"/api/v1/tenants/{tenant}/products/{product.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidPrice_ReturnsStableProblemCode()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/tenants/tenant-a/products", new CreateProductRequest("PRO", "Pro", -1m, "USD"));
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("price.negative", problem?["code"].ToString());
    }
}
