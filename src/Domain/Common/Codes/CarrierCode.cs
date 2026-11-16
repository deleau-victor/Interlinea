namespace Domain.Common.Codes;

/// <summary>
/// IATA carrier code: 2 Uppercase letters (e.g. "AF", "PU", "U2").
/// </summary>
public readonly record struct CarrierCode
{
    public string Value { get; }

    public CarrierCode(string value)
    {
        if (value is not { Length: 2 } || !value.All(c => char.IsAsciiLetterUpper(c) || char.IsAsciiDigit(c)))
            throw new ArgumentException($"Invalid IATA carrier code: '{value}'.", nameof(value));
        Value = value;
    }

    public static implicit operator CarrierCode(string value) => new(value);
    public override string ToString() => Value;
}
