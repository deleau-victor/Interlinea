namespace Domain.Search.Planning.Partitions;

/// <summary>
/// The direction of the travel
/// </summary>
public enum TravelDirection
{
    /// <summary>
    /// The outward, from the user's home to the destination
    /// </summary>
    Outward,

    /// <summary>
    /// The return, from the destination to the user home's
    /// </summary>
    Return,
}
