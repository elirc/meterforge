using MeterForge.Domain.Common;

namespace MeterForge.Domain.Catalog;

public sealed class Product
{
    public Product(Guid id, string tenantId, string code, string name, Money monthlyPrice)
    {
        if (id == Guid.Empty) throw new BusinessRuleException("product.id_required", "Product ID is required.");
        if (string.IsNullOrWhiteSpace(tenantId)) throw new BusinessRuleException("tenant.required", "Tenant is required.");
        if (string.IsNullOrWhiteSpace(code)) throw new BusinessRuleException("product.code_required", "Code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleException("product.name_required", "Name is required.");
        if (monthlyPrice.Amount < 0) throw new BusinessRuleException("price.negative", "Price cannot be negative.");

        Id = id;
        TenantId = tenantId.Trim();
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        MonthlyPrice = monthlyPrice;
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public string Code { get; }
    public string Name { get; }
    public Money MonthlyPrice { get; }
}
