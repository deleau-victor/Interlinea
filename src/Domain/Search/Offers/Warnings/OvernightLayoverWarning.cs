namespace Domain.Search.Offers.Warnings;

/// <summary>
/// A layover between flights requires spending the night there.
/// </summary>
/// <param name="At">The airport of the layover</param>
/// <param name="Layover">The time of the layover</param>
public sealed record OvernightLayoverWarning(AirportCode At, TimeSpan Layover) : OfferWarning;
