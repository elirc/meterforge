using MeterForge.Application.Catalog;
using MeterForge.Contracts.V1.Catalog;
using MeterForge.Domain.Catalog;

namespace MeterForge.Api.Catalog;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/tenants/{tenantId}/products").WithTags("Products");

        group.MapPost("/", async (string tenantId, CreateProductRequest request, ProductService service, CancellationToken cancellationToken) =>
        {
            var product = await service.CreateAsync(tenantId, request.Code, request.Name, request.MonthlyAmount, request.Currency, cancellationToken);
            return Results.Created($"/api/v1/tenants/{tenantId}/products/{product.Id}", Map(product));
        });

        group.MapGet("/{productId:guid}", async (string tenantId, Guid productId, ProductService service, CancellationToken cancellationToken) =>
        {
            var product = await service.GetAsync(tenantId, productId, cancellationToken);
            return product is null ? Results.NotFound() : Results.Ok(Map(product));
        });

        return endpoints;
    }

    private static ProductResponse Map(Product product) => new(
        product.Id,
        product.TenantId,
        product.Code,
        product.Name,
        new MoneyResponse(product.MonthlyPrice.Amount, product.MonthlyPrice.Currency));
}
