using MeterForge.Domain.Common;

namespace MeterForge.Domain.Pricing;

public sealed record PriceVersion
{
    public PriceVersion(Guid id, Guid productId, Money unitPrice, DateOnly validFrom, DateOnly? validTo)
    {
        if (id == Guid.Empty || productId == Guid.Empty) throw new BusinessRuleException("price.identity_required", "Price and product IDs are required.");
        if (validTo <= validFrom) throw new BusinessRuleException("price.range_invalid", "Price end must be after its start.");
        Id = id;
        ProductId = productId;
        UnitPrice = unitPrice;
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public Guid Id { get; }
    public Guid ProductId { get; }
    public Money UnitPrice { get; }
    public DateOnly ValidFrom { get; }
    public DateOnly? ValidTo { get; }

    public bool AppliesOn(DateOnly date) => date >= ValidFrom && (ValidTo is null || date < ValidTo);
}

public static class PriceSelector
{
    public static PriceVersion Select(IReadOnlyCollection<PriceVersion> versions, Guid productId, DateOnly date)
    {
        var matches = versions.Where(version => version.ProductId == productId && version.AppliesOn(date)).ToArray();
        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new BusinessRuleException("price.not_found", "No price applies on the requested date."),
            _ => throw new BusinessRuleException("price.overlap", "Multiple prices apply on the requested date."),
        };
    }
}
