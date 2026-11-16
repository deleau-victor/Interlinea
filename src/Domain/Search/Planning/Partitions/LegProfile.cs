namespace Domain.Search.Planning.Partitions;

/// <summary>
/// What the worker measured on one side of a partition. Both floors are totals for that side
/// in both directions, so that the two pricing modes compare on the same scale.
/// </summary>
/// <param name="OneWaysFloor">
/// The cheapest one-way out on this side, added to the cheapest one-way back on the SAME side;
/// null when the side was not probed in that mode, or nothing was found in one of the two
/// directions.
/// </param>
/// <param name="RoundTripFloor">The lowest price measured for a RT; null if not measured or not found.</param>
/// <param name="FlightsPerDay">
/// The observed average of distinct journeys per day on this side, as one division of two
/// counts. Anchoring compares the two sides of a profile for equality, and that comparison is
/// exact because both ratios come from the same division in the same place: two journeys over
/// six days and one over three are the same double, bit for bit. Storing the value in a column
/// that rounds does NOT break it, since both sides are rounded alike. What would break it is
/// refreshing one side without the other, so that one frequency is read back from the store
/// and the other recomputed.
/// </param>
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
