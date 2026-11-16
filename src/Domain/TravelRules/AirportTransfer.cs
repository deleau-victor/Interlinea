namespace Domain.TravelRules;

/// <summary>Ground Transfer between two airports of the same city (Orly -> Roissy, Ezeiza -> Aeroparque).</summary>
/// <param name="From">The arrival airport of the first flight</param>
/// <param name="To">the departure airport of the next flight</param>
/// <param name="Duration">Estimated transfer time</param>
/// <param name="Cost">Estimated cost of the transfer</param>
public sealed record AirportTransfer(AirportCode From, AirportCode To, TimeSpan Duration, Money Cost);
