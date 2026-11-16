namespace Domain.Search.Planning.Partitions;

/// <summary>
/// How a segment is booked for a trip.
/// The virtual interlining is based on pricing difference between :
/// - long-haul where the carriers sells RT cheaper than 2 OW
/// - short-haul where the carriers sells RT at almost same price than 2 OW
/// </summary>
public enum LegPricing
{
    OneWays,
    RoundTrip
}
