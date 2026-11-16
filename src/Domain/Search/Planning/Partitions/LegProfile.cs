namespace Domain.Search.Planning.Partitions;

/// <summary>
/// What the worker measured on a segment
/// </summary>
/// <param name="OneWaysFloor">The lowest cost for a OW in a side + the lowest in the other side; null if not measured or not found.</param>
/// <param name="RoundTripFloor">The lowest price measured for a RT; null if not measured or not found.</param>
/// <param name="FlightsPerDay">The observed average distinct flights per day on this segment</param>
public sealed record LegProfile(Money? OneWaysFloor, Money? RoundTripFloor, double FlightsPerDay)
{
    /// <summary>
    /// Get the price floor for the requested paying mode
    /// </summary>
    /// <param name="pricing">The paying mode</param>
    /// <returns>The floor for the requested mode</returns>
    public Money? FloorFor(LegPricing pricing) => pricing == LegPricing.RoundTrip
        ? RoundTripFloor
        : OneWaysFloor;
}
