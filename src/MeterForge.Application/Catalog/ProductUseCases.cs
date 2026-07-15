using MeterForge.Domain.Catalog;
using MeterForge.Domain.Common;

namespace MeterForge.Application.Catalog;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken);
    Task<Product?> GetAsync(string tenantId, Guid productId, CancellationToken cancellationToken);
}

public sealed class ProductService(IProductRepository repository)
{
    public async Task<Product> CreateAsync(
        string tenantId,
        string code,
        string name,
        decimal monthlyAmount,
        string currency,
        CancellationToken cancellationToken)
    {
        var product = new Product(Guid.NewGuid(), tenantId, code, name, new Money(monthlyAmount, currency));
        await repository.AddAsync(product, cancellationToken).ConfigureAwait(false);
        return product;
    }

    public Task<Product?> GetAsync(string tenantId, Guid productId, CancellationToken cancellationToken) =>
        repository.GetAsync(tenantId, productId, cancellationToken);
}
