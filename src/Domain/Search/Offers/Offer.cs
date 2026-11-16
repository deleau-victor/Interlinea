using Domain.Fares;
using Domain.Search.Offers.AncillaryCosts;
using Domain.Search.Offers.Connections;
using Domain.Search.Offers.Warnings;
using Domain.Search.Planning.Partitions;

namespace Domain.Search.Offers;

/// <summary>
/// An offer is composed by one or many tickets, and known the orders of them.
/// It serve to rank them and display them to the user
/// </summary>
public sealed record Offer
{
    /// <summary>
    /// The list of the tickets to buy for this offer
    /// </summary>
    public IReadOnlyList<OfferedTicket> Tickets { get; }

    /// <summary>
    /// The outbound leg of the trip, in order, from the departure airport to the destination
    /// </summary>
    public IReadOnlyList<Journey> Outbound { get; }

    /// <summary>
    /// The return leg of the trip, in order, from the destination to the arrival airport.
    /// </summary>
    public IReadOnlyList<Journey> Inbound { get; }

    /// <summary>
    /// All stopovers, whether protected or self-transfer, order is deducted by the orders of legs
    /// </summary>
    public IReadOnlyList<Connection> Connections { get; }

    /// <summary>
    /// The partition who create the offer <see langword="null"/> pour un billet unique
    /// </summary>
    public Partition? Via { get; }

    /// <summary>
    /// The ancillary cost outside of the tickets (document, transfer, hotel)
    /// </summary>
    public IReadOnlyList<AncillaryCost> AncillaryCosts { get; init; } = [];

    /// <summary>
    /// The warnings to display on the offer
    /// </summary>
    public IReadOnlyList<OfferWarning> Warnings { get; init; } = [];

    public Offer(IReadOnlyList<OfferedTicket> tickets, IReadOnlyList<Journey> outbound, IReadOnlyList<Journey> inbound, Partition? via)
    {
        if (tickets.Count == 0)
            throw new ArgumentException("An offer contains at least one ticket.", nameof(tickets));
        if (outbound.Count == 0 || inbound.Count == 0)
            throw new ArgumentException("An offer must contain at least an outbound.", nameof(outbound));
        if (tickets.Select(t => t.Selected.Price.Currency).Distinct().Count() > 1)
            throw new ArgumentException("All tickets in an offer must are in the same currency.", nameof(tickets));

        var bought = tickets.SelectMany(t => t.Selected.Journeys).ToHashSet(ReferenceEqualityComparer.Instance);
        var placed = outbound.Concat(inbound).ToList();
        if (placed.Count != bought.Count || placed.Distinct(ReferenceEqualityComparer.Instance).Count() != placed.Count
            || !placed.All(bought.Contains))
            throw new ArgumentException("Each leg of the trip must appear once, either on the outbound or return leg.", nameof(outbound));

        EnsureChronological(outbound, nameof(outbound));
        EnsureChronological(inbound, nameof(inbound));
        if (inbound[0].DepartureAt <= outbound[^1].ArrivalAt)
            throw new ArgumentException("The return trip must depart after the arrival of the outbound trip.", nameof(inbound));

        (Tickets, Outbound, Inbound, Via) = (tickets.ToArray(), outbound.ToArray(), inbound.ToArray(), via);
        Connections = Derive(Outbound).Concat(Derive(Inbound)).ToArray();
    }

    /// <summary>
    /// The currency of the offer
    /// </summary>
    public string Currency => Tickets[0].Selected.Price.Currency;

    /// <summary>
    /// The total prices of the tickets
    /// </summary>
    public Money TicketsPrice => Money.Sum(Tickets.Select(t => t.Selected.Price));

    /// <summary>
    /// The total price of the ancillary costs, zero when there are none.
    /// </summary>
    public Money AncillaryTotal => Money.Sum(AncillaryCosts.Select(c => c.Amount), Currency);

    /// <summary>
    /// The total cost of the travel
    /// </summary>
    public Money TotalCost => TicketsPrice + AncillaryTotal;

    /// <summary>Is the offer combining many tickets (virtual interlining) ?</summary>
    public bool IsVirtualInterlining => Tickets.Count > 1;

    /// <summary>
    /// The duration of the outbound leg of the trip
    /// </summary>
    public TimeSpan OutboundDuration => Outbound[^1].ArrivalAt - Outbound[0].DepartureAt;

    /// <summary>
    /// The duration of the inbound leg of the trip
    /// </summary>
    public TimeSpan InboundDuration => Inbound[^1].ArrivalAt - Inbound[0].DepartureAt;

    /// <summary>
    /// The total number of layovers, inbound and outbound
    /// </summary>
    public int Stops => Connections.Count;

    /// <summary>
    /// Numbers of layovers in Self-Transfer
    /// </summary>
    public int SelfTransfers => Connections.Count(c => c.Kind == ConnectionKind.SelfTransfer);

    private static void EnsureChronological(IReadOnlyList<Journey> sequence, string name)
    {
        for (int i = 1; i < sequence.Count; i++)
            if (sequence[i].DepartureAt <= sequence[i - 1].ArrivalAt)
                throw new ArgumentException("Routes that go in the same direction must follow one another.", name);
    }

    private static IEnumerable<Connection> Derive(IReadOnlyList<Journey> sequence)
    {
        for (int i = 0; i < sequence.Count; i++)
        {
            if (i > 0)
                yield return new Connection(sequence[i - 1].Segments[^1], sequence[i].Segments[0], ConnectionKind.SelfTransfer);

            IReadOnlyList<Segment> segments = sequence[i].Segments;
            for (int s = 1; s < segments.Count; s++)
                yield return new Connection(segments[s - 1], segments[s], ConnectionKind.Protected);
        }
    }
}
