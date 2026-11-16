namespace Domain.Fares;

/// <summary>
/// Represent a ticket bought in one time at a seller.
/// Can be a one-way ticket, or a round trip ticket.
/// The ticket journey's can contains many segments and the stopover between segments are protected because there are one ticket journeys.
/// A ticket is valable for 24 hours.
/// </summary>
/// <param name="Id">The unique identifier of the ticket</param>
/// <param name="Journeys">One or two journeys covered by the ticket</param>
/// <param name="Price">The ticket price</param>
/// <param name="Seller">The seller of the ticket</param>
/// <param name="CheckedBagIncluded">Indicates wether if a checked baggage is included in the price</param>
/// <param name="ObservedAt">The date where this ticket with this price was observed</param>
public sealed record Ticket(
    Guid Id,
    IReadOnlyList<Journey> Journeys,
    Money Price,
    Seller Seller,
    bool CheckedBagIncluded,
    DateTimeOffset ObservedAt)
{
    /// <summary>
    /// The validated journeys of the ticket
    /// </summary>
    public IReadOnlyList<Journey> Journeys { get; } = Validate(Journeys);

    /// <summary>
    /// The booking Uri for this ticket
    /// </summary>
    public Uri? BookingLink { get; init; }

    /// <summary>
    /// The trip type of this ticket : One way or Round Trip
    /// </summary>
    public TripType TripType => Journeys.Count == 1 ? TripType.OneWay : TripType.RoundTrip;

    /// <summary>
    /// The first journey of the ticket
    /// Exemple : CDG -> JFK
    /// </summary>
    public Journey FirstJourney => Journeys[0];

    /// <summary>
    /// The second journey of the ticket, if there one.
    /// Exemple JFK -> CDG
    /// </summary>
    public Journey? ReturnJourney => Journeys.Count == 2 ? Journeys[1] : null;

    /// <summary>
    /// All the segments of this ticket.
    /// </summary>
    public IEnumerable<Segment> Segments => Journeys.SelectMany(j => j.Segments);

    /// <summary>
    /// The kind of the ticket. Can be :
    /// - Online : the same carrier operate all the segments
    /// - Codeshare : the carrier, or another carrier sharing the same flight code can operate the flight
    /// - Interline : several compagnies operates on different flights of this journey
    /// </summary>
    public TicketKind Kind =>
        Segments.Select(s => s.Designator.Carrier).Distinct().Count() > 1 ? TicketKind.Interline
            : Segments.Any(s => s.IsCodeshare) ? TicketKind.Codeshare
            : TicketKind.Online;

    /// <summary>
    /// The unique key of this ticket
    /// </summary>
    public string Key => string.Join(" | ", Journeys.Select(j => j.Key));

    /// <summary>
    /// Indicate wether this ticket is fresh, and valuable
    /// </summary>
    /// <param name="now"></param>
    /// <param name="ttl"></param>
    /// <returns></returns>
    public bool IsFresh(DateTimeOffset now, TimeSpan ttl) => now - ObservedAt <= ttl;

    private static Journey[] Validate(IReadOnlyList<Journey> journeys)
    {
        if (journeys.Count is not (1 or 2))
            throw new ArgumentException("A ticket can only cover a one-way, or a round trip", nameof(journeys));

        if (journeys.Count == 2 && journeys[1].DepartureAt <= journeys[0].ArrivalAt)
            throw new ArgumentException("The return must leave after the arrival of the outward journey.", nameof(journeys));

        return [.. journeys];
    }
}
