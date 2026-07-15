namespace MeterForge.Domain.Common;

public readonly record struct Money
{
    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new BusinessRuleException("money.currency_invalid", "Currency must be a three-letter code.");
        }

        if (decimal.Round(amount, 2, MidpointRounding.ToEven) != amount)
        {
            throw new BusinessRuleException("money.precision_invalid", "Money supports at most two decimal places.");
        }

        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new BusinessRuleException("money.currency_mismatch", "Unlike currencies cannot be added.");
        }

        return new Money(left.Amount + right.Amount, left.Currency);
    }
}
