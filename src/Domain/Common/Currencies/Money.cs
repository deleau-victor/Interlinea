namespace Domain.Common.Currencies;

/// <summary>
/// Represents a monetary value with an amount and a currency code.
/// </summary>
/// <param name="Amount">The monetary amount, which must be non-negative.</param>
/// <param name="Currency">The ISO 4217 currency code.</param>
public readonly record struct Money(decimal Amount, string Currency) : IComparable<Money>
{
    #region  ——————————— Properties ———————————
    /// <summary>
    /// The monetary amount, which must be non-negative.
    /// </summary>
    public decimal Amount { get; } = Amount >= 0
        ? Amount
        : throw new ArgumentOutOfRangeException(nameof(Amount), Amount, "Amount must be non-negative.");

    /// <summary>
    /// The ISO 4217 currency code.
    /// </summary>
    public string Currency { get; } = Currency is { Length: 3 } && Currency.All(char.IsAsciiLetterUpper)
        ? Currency
        : throw new ArgumentException($"Invalid ISO 4217 currency code: '{Currency}'.", nameof(Currency));
    #endregion

    #region ——————————— Methods ———————————
    /// <summary>
    /// Sums a collection of Money values, ensuring they all have the same currency.
    /// </summary>
    /// <param name="items">The collection of Money values to sum.</param>
    /// <returns>The total sum of the Money values.</returns>
    /// <exception cref="ArgumentException">Thrown if the collection is empty.</exception>
    public static Money Sum(IEnumerable<Money> items)
    {
        using IEnumerator<Money> e = items.GetEnumerator();
        if (!e.MoveNext())
            throw new ArgumentException("Cannot sum an empty collection.", nameof(items));
        Money total = e.Current;
        while (e.MoveNext())
            total += e.Current;
        return total;
    }

    /// <summary>
    /// Compares this Money instance with another, ensuring they have the same currency.
    /// </summary>
    /// <param name="other">The other Money instance to compare with.</param>
    /// <returns>A value indicating the relative order of the instances.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the currencies of the two Money instances are different.</exception>
    public int CompareTo(Money other)
    {
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }
    #endregion

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot operate on Money values with different currencies: '{a.Currency}' and '{b.Currency}'.");
    }

    #region —————————— Operators ———————————
    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        decimal resultAmount = left.Amount - right.Amount;
        if (resultAmount < 0)
            throw new InvalidOperationException($"Resulting amount cannot be negative: {left.Amount} - {right.Amount}.");
        return new Money(resultAmount, left.Currency);
    }

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;
    #endregion

    public override string ToString() => $"{Amount:0.##} {Currency}";
}
