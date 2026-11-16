namespace Domain.Search.Planning.Partitions;

/// <summary>
/// The side of the segment relatively to the trip
/// </summary>
public enum TripSide
{
    /// <summary>
    /// Between the origin airport and the hub.
    /// Example : Paris -> Madrid (where Paris is the starting point and Madrid is not the destination)
    /// </summary>
    Origin,

    /// <summary>
    /// Between the destination and the hub.
    /// Example : Madrid -> Lima (Where Lima is the destination point and Madrid il not the starting point)
    /// </summary>
    Destination,
}
