namespace Domain.Search;

/// <summary>
/// Represents a user's multi-criteria flight search request.
/// </summary>
/// <param name="Origins">The list of accepted departure airports</param>
/// <param name="Destination">The target arrival airport code</param>
/// <param name="OutboundDates">The flexible or exact date range allowed for the outbound journey</param>
/// <param name="ReturnDates">The flexible or exact date range allowed for the return journey</param>
/// <param name="Baggage">Cabin only or checked baggage option</param>
/// <param name="Currency">The requested ISO 4217 currency code (e.g., "EUR", "USD")</param>
/// <param name="RequestedAt">The exact timestamp when the request was initiated</param>
public sealed record SearchRequest(
    IReadOnlySet<AirportCode> Origins,
    AirportCode Destination,
    DateRange OutboundDates,
    DateRange ReturnDates,
    BaggageOption Baggage,
    string Currency,
    DateTimeOffset RequestedAt)
{
    /// <summary>
    /// Gets the set of departure airports.
    /// Enforces that at least one origin is specified.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the origin set is empty.</exception>
    public IReadOnlySet<AirportCode> Origins { get; } = Origins.Count > 0
        ? Origins
        : throw new ArgumentException("At least one departure airport must be chosen", nameof(Origins));

    /// <summary>
    /// Gets the target destination airport.
    /// Enforces that the destination is not present in the origins set.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the destination matches one of the departure airports.</exception>
    public AirportCode Destination { get; } = !Origins.Contains(Destination)
        ? Destination
        : throw new ArgumentException("The destination cannot be same as one of the origins", nameof(Destination));

    /// <summary>
    /// Gets the return journey date range.
    /// Enforces that the return timeline does not start before the outbound timeline.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the return start date precedes the outbound start date.</exception>
    public DateRange ReturnDates { get; } = ReturnDates.From >= OutboundDates.From
        ? ReturnDates
        : throw new ArgumentException("The return cannot precede the departure.", nameof(ReturnDates));
}
