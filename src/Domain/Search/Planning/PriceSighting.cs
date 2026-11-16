using Domain.Fares;

namespace Domain.Search.Planning;

/// <summary>
/// A price seen on a route while probing, in answer to a <see cref="PriceQuery"/>.
/// </summary>
/// <remarks>
/// Deliberately not a <see cref="Ticket"/>. Probing reads the prices a search engine lists,
/// and those are not the prices confirmed on a booking panel; confirming each one costs a page
/// load, which the probe budget does not allow for a sweep whose only purpose is to measure a
/// floor. A sighting therefore carries no seller, no booking link and no included services —
/// nothing that would let it be composed into an offer, or kept in the price history that
/// tickets feed. It is a measurement, not merchandise.
/// </remarks>
/// <param name="Journeys">The journey sighted, or the two journeys of a round trip.</param>
/// <param name="Price">The price listed for them, in the currency of the search that probed.</param>
public sealed record PriceSighting(IReadOnlyList<Journey> Journeys, Money Price)
{
    /// <summary>
    /// The validated journeys of the sighting.
    /// </summary>
    public IReadOnlyList<Journey> Journeys { get; } = JourneyPair.Validate(Journeys, nameof(Journeys));

    /// <summary>
    /// The journey sighted, or the outbound one when the sighting is a round trip.
    /// </summary>
    public Journey FirstJourney => Journeys[0];

    /// <summary>
    /// The return journey, or <see langword="null"/> when the sighting is a one-way.
    /// </summary>
    public Journey? ReturnJourney => Journeys.Count == 2 ? Journeys[1] : null;
}
