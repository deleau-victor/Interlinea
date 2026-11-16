namespace Domain.Common.Codes;

/// <summary>
/// ISO 3166-1 alpha-2 country code: 2 Uppercase letters (e.g. "FR", "US", "JP").
/// </summary>
public readonly record struct CountryCode
{
    public string Value { get; }

    public CountryCode(string value)
    {
        if (value is not { Length: 2 } || !value.All(char.IsAsciiLetterUpper))
            throw new ArgumentException($"Invalid ISO country code: '{value}'.", nameof(value));
        Value = value;
    }

    public static implicit operator CountryCode(string value) => new(value);
    public override string ToString() => Value;
}
