namespace Domain.Common.Codes;

/// <summary>
/// IATA airport code: 3 Uppercase letters (e.g. "CDG", "JFK", "LAX").
/// </summary>
public readonly record struct AirportCode
{
    public string Value { get; }

    public AirportCode(string value)
    {
        if (value is not { Length: 3 } || !value.All(char.IsAsciiLetterUpper))
            throw new ArgumentException($"Invalid IATA airport code: '{value}'.", nameof(value));
        Value = value;
    }

    public static implicit operator AirportCode(string value) => new(value);
    public override string ToString() => Value;
}
