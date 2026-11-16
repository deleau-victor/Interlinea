namespace Domain.Common.Currencies;

/// <summary>
/// Table of the exchange rates between different currencies.
/// Loaded from the API and updated daily.
/// </summary>
public sealed class ExchangeRates
{
    private readonly Dictionary<(string From, string To), decimal> _rates = [];

    public ExchangeRates(IEnumerable<(string From, string To, decimal Rate)> rates)
    {
        foreach ((string? from, string? to, decimal rate) in rates)
        {
            if (rate <= 0)
                throw new ArgumentOutOfRangeException(nameof(rates), "Th exchange rate need to be positive.");
            _rates[(from, to)] = rate;
        }
    }

    /// <summary>
    /// Convert an amount from one currency to another using the exchange rates.
    /// </summary>
    /// <param name="money">The money to convert.</param>
    /// <param name="currency">The target currency code.</param>
    /// <returns>The converted money in the target currency.</returns>
    /// <exception cref="InvalidOperationException">Thrown if there is no exchange rate available for the given currency pair.</exception>
    public Money Convert(Money money, string currency)
    {
        if (money.Currency == currency)
            return money;
        if (_rates.TryGetValue((money.Currency, currency), out decimal rate))
            return new Money(Round(money.Amount * rate), currency);
        if (_rates.TryGetValue((currency, money.Currency), out decimal inverse))
            return new Money(Round(money.Amount / inverse), currency);
        throw new InvalidOperationException($"No exchange rate available for converting from '{money.Currency}' to '{currency}'.");
    }

    private static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
