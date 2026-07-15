using System.Collections.Concurrent;
using MeterForge.Application.Catalog;
using MeterForge.Domain.Catalog;

namespace MeterForge.Infrastructure.Catalog;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<(string TenantId, Guid ProductId), Product> _products = new();

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _products[(product.TenantId, product.Id)] = product;
        return Task.CompletedTask;
    }

    public Task<Product?> GetAsync(string tenantId, Guid productId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _products.TryGetValue((tenantId, productId), out var product);
        return Task.FromResult(product);
    }
}
