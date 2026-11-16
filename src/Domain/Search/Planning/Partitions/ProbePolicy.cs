namespace Domain.Search.Planning.Partitions;

/// <summary>
/// The policy which provide the threshold to decides which buying mode (OW or RT) to try fort a trip.
/// Exist because, a long-haul flight are generally cheaper from a RT, and it doesn't have this incidence in short-haul flights.
/// </summary>
/// <param name="OneWaysOnlyBelowKm">The threshold below the flights will only be searched in OW</param>
/// <param name="RoundTripOnlyAboveKm">The threshold above the flights will only be searched in RT</param>
public sealed record ProbePolicy(double OneWaysOnlyBelowKm, double RoundTripOnlyAboveKm)
{
    public double RoundTripOnlyAboveKm { get; } = RoundTripOnlyAboveKm >= OneWaysOnlyBelowKm
        ? RoundTripOnlyAboveKm
        : throw new ArgumentException("The long-haul threshold must exceed the short-haul threshold.", nameof(RoundTripOnlyAboveKm));

    /// <summary>
    /// The pricing modes to try for a segment
    /// </summary>
    /// <param name="distanceKm">the distance of the segment</param>
    /// <returns>The modes to try</returns>
    public IReadOnlyList<LegPricing> ModesFor(double distanceKm) => distanceKm < OneWaysOnlyBelowKm
        ? [LegPricing.OneWays]
        : distanceKm > RoundTripOnlyAboveKm
            ? [LegPricing.RoundTrip]
            : [LegPricing.OneWays, LegPricing.RoundTrip];
}
