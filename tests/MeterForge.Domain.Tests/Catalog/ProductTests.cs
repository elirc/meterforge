using MeterForge.Domain.Catalog;
using MeterForge.Domain.Common;

namespace MeterForge.Domain.Tests.Catalog;

public sealed class ProductTests
{
    [Fact]
    public void Create_NormalizesCodeAndCurrency()
    {
        var product = new Product(Guid.NewGuid(), "tenant-a", " pro ", "Pro", new Money(49.99m, "usd"));

        Assert.Equal("PRO", product.Code);
        Assert.Equal("USD", product.MonthlyPrice.Currency);
    }

    [Fact]
    public void Create_NegativePrice_RejectsRule()
    {
        var error = Assert.Throws<BusinessRuleException>(() =>
            new Product(Guid.NewGuid(), "tenant-a", "PRO", "Pro", new Money(-1m, "USD")));

        Assert.Equal("price.negative", error.Code);
    }

    [Fact]
    public void Add_DifferentCurrencies_RejectsRule()
    {
        var error = Assert.Throws<BusinessRuleException>(() => new Money(10m, "USD") + new Money(10m, "EUR"));

        Assert.Equal("money.currency_mismatch", error.Code);
    }
}
