namespace Domain.Search.Offers.AncillaryCosts;

/// <summary>
/// The kind of a cost outside of the ticket
/// </summary>
public enum AncillaryKind
{
    /// <summary>
    /// Document required for a layover (ESTA, eTA, etc.), if the traveler does not already have it.
    /// </summary>
    TravelDocument,

    /// <summary>
    /// Ground transportation between two airports in the same city.
    /// </summary>
    GroundTransfer,

    /// <summary>
    /// A hotel stay necessitated by a long wait between flights.
    /// </summary>
    HotelNight
}
