namespace MeterForge.Contracts.V1.Catalog;

public sealed record CreateProductRequest(string Code, string Name, decimal MonthlyAmount, string Currency);

public sealed record MoneyResponse(decimal Amount, string Currency);

public sealed record ProductResponse(Guid Id, string TenantId, string Code, string Name, MoneyResponse MonthlyPrice);
