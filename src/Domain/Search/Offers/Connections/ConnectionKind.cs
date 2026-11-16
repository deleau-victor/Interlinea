namespace Domain.Search.Offers.Connections;

/// <summary>The layover kind</summary>
public enum ConnectionKind
{
    /// <summary>
    /// Protected connection flight: Both flights are part of the same itinerary on a single ticket.
    /// If the first flight is delayed, the airline must rebook the passenger.
    /// </summary>
    Protected,

    /// <summary>
    /// Self-transfer: The two flights are on separate tickets. No airline guarantees a connection.
    /// If a delay causes you to miss the second flight, it is at your own risk (hence the recommendation to purchase insurance).
    /// </summary>
    SelfTransfer,
}
