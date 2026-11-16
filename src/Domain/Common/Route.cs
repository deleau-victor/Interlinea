namespace Domain.Common;

/// <summary>
/// Represents a route between two airports, identified by their IATA airport codes.
/// </summary>
/// <param name="From">The IATA airport code of the departure airport.</param>
/// <param name="To">The IATA airport code of the arrival airport.</param>
public readonly record struct Route(AirportCode From, AirportCode To)
{
    public override string ToString() => $"{From}→{To}";
}
