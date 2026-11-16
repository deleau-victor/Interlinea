namespace Domain.Search.Planning.Partitions;

/// <summary>
/// The way to cut a trip around a hub to build self transfer offers.
/// </summary>
/// <param name="Hub">The airport where the users take his stopover</param>
/// <param name="OriginSidePricing">The buying mode of the ticket (RT or OW) for the origin side</param>
/// <param name="DestinationSidePricing">The buying mode of the ticket (RT or OW) for the destination side</param>
public sealed record Partition(
    AirportCode Hub,
    LegPricing OriginSidePricing,
    LegPricing DestinationSidePricing)
{
    /// <summary>
    /// The buying mode of a side (RT or OW)
    /// </summary>
    /// <param name="side">A segment side</param>
    /// <returns>The buy mode of the segment side</returns>
    public LegPricing PricingOf(TripSide side) => side == TripSide.Origin
        ? OriginSidePricing
        : DestinationSidePricing;

    /// <summary>
    /// In a given direction, does this side come before the other ?
    /// </summary>
    /// <param name="side">The side we want to know if it come first</param>
    /// <param name="direction">The travel direction</param>
    /// <returns>A boolean indicates if it comes first</returns>
    public static bool ComesFirst(TripSide side, TravelDirection direction) =>
        side == TripSide.Origin == (direction == TravelDirection.Outward);

    /// <inheritdoc/>
    public override string ToString() =>
        $"By {Hub} ({Describe(OriginSidePricing)} in origin side, {Describe(DestinationSidePricing)} destination side)";

    private static string Describe(LegPricing pricing) => pricing == LegPricing.RoundTrip ? "RT" : "OW";
}
