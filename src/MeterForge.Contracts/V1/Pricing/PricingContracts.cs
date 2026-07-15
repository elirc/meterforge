namespace MeterForge.Contracts.V1.Pricing;

public sealed record PriceVersionInput(Guid Id, decimal Amount, string Currency, DateOnly ValidFrom, DateOnly? ValidTo);
public sealed record SelectPriceRequest(Guid ProductId, DateOnly AsOf, IReadOnlyCollection<PriceVersionInput> Versions);
public sealed record SelectedPriceResponse(Guid PriceId, decimal Amount, string Currency, DateOnly AsOf);
