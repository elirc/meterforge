using MeterForge.Application.Catalog;
using MeterForge.Infrastructure.Catalog;

namespace MeterForge.Application.Tests.Catalog;

public sealed class ProductServiceTests
{
    [Fact]
    public async Task CreateThenGet_SameTenant_ReturnsProduct()
    {
        var service = new ProductService(new InMemoryProductRepository());
        var created = await service.CreateAsync("tenant-a", "PRO", "Pro", 49m, "USD", CancellationToken.None);

        var found = await service.GetAsync("tenant-a", created.Id, CancellationToken.None);

        Assert.Same(created, found);
    }

    [Fact]
    public async Task Get_ForeignTenant_ReturnsNull()
    {
        var service = new ProductService(new InMemoryProductRepository());
        var created = await service.CreateAsync("tenant-a", "PRO", "Pro", 49m, "USD", CancellationToken.None);

        var found = await service.GetAsync("tenant-b", created.Id, CancellationToken.None);

        Assert.Null(found);
    }
}
