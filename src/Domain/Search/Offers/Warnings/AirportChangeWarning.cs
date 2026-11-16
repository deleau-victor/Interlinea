namespace Domain.Search.Offers.Warnings;

/// <summary>A layover that involves changing airports.</summary>
/// <param name="From">Arrival Airport</param>
/// <param name="To">The next departure airport</param>
public sealed record AirportChangeWarning(AirportCode From, AirportCode To) : OfferWarning;
