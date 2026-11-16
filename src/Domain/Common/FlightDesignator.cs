namespace Domain.Common;

/// <summary>
/// Represents a flight by its IATA designation: a carrier code, a flight number, and an optional operational suffix.
/// </summary>
public readonly record struct FlightDesignator
{
    /// <summary>
    /// The IATA carrier code (2 uppercase letters) of the airline operating the flight.
    /// </summary>
    public CarrierCode Carrier { get; }

    /// <summary>
    /// The flight number (1 to 4 digits) assigned by the airline.
    /// </summary>
    public int Number { get; }

    /// <summary>
    /// An optional operational suffix (1 uppercase letter) used to differentiate flights with the same carrier and number.
    /// </summary>
    public char? Suffix { get; }

    public FlightDesignator(CarrierCode carrier, int number, char? suffix = null)
    {
        if (number is < 1 or > 9999)
            throw new ArgumentOutOfRangeException(nameof(number), "A flight number consists of 1 to 4 digits.");

        if (suffix is { } s && !char.IsAsciiLetterUpper(s))
            throw new ArgumentException("The operational suffix must be an uppercase letter.", nameof(suffix));

        (Carrier, Number, Suffix) = (carrier, number, suffix);
    }

    /// <summary>
    /// Parses a string representation of a flight designator into a FlightDesignator instance.
    /// </summary>
    /// <param name="value">The string unformatted flight designator (e.g., "AF123", "AF 123A").</param>
    /// <returns>A FlightDesignator instance representing the parsed flight designator.</returns>
    /// <exception cref="FormatException">Thrown if the input string is not a valid flight designator.</exception>
    public static FlightDesignator Parse(string value)
    {
        string trimmed = value.Replace(" ", "", StringComparison.Ordinal);
        if (trimmed.Length < 3)
            throw new FormatException($"Invalid flight number: '{value}'.");

        string rest = trimmed[2..];
        char? suffix = char.IsAsciiLetterUpper(rest[^1]) ? rest[^1] : null;
        string digits = suffix is null ? rest : rest[..^1];
        if (digits.Length is 0 or > 4 || !digits.All(char.IsAsciiDigit))
            throw new FormatException($"Invalid flight number: '{value}'.");

        CarrierCode carrier = new(trimmed[..2]);
        int number = int.Parse(digits, CultureInfo.InvariantCulture);

        return new FlightDesignator(carrier, number, suffix);
    }

    public static implicit operator FlightDesignator(string value) => Parse(value);
    public override string ToString() => $"{Carrier} {Number}{Suffix}";
}
