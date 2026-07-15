namespace MeterForge.Contracts.V1.Adjustments;

public sealed record DiscountPreviewRequest(decimal Amount, string Currency, string Code, decimal Percent);
public sealed record DiscountPreviewResponse(string Code, decimal Original, decimal Discount, decimal Net, string Currency);
