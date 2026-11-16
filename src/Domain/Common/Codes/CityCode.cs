namespace Domain.Common.Codes;

/// <summary>
/// IATA city code: 3 Uppercase letters (e.g. "PAR", "MAD", "BRU").
/// A IATA city code contains many airports e.g. PAR = CDG + ORY
/// </summary>
public readonly record struct CityCode
{
    public string Value { get; }

    public CityCode(string value)
    {
        if (value is not { Length: 3 } || !value.All(char.IsAsciiLetterUpper))
            throw new ArgumentException($"Invalid IATA city code: '{value}'.", nameof(value));
        Value = value;
    }

    public static implicit operator CityCode(string value) => new(value);
    public override string ToString() => Value;
}
