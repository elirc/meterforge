using MeterForge.Domain.Common;

namespace MeterForge.Application.Tax;

public sealed record TaxQuote(string ProviderReference, Money Tax, string Jurisdiction, string CalculationVersion);

public interface ITaxQuoteProvider
{
    Task<TaxQuote> QuoteAsync(string tenantId, string countryCode, Money taxableAmount, DateOnly asOf, CancellationToken cancellationToken);
}
