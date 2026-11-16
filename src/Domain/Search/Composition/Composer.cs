using Domain.Fares;
using Domain.Reference.Airports;
using Domain.Search.Offers;
using Domain.Search.Offers.AncillaryCosts;
using Domain.Search.Offers.Connections;
using Domain.Search.Offers.Warnings;
using Domain.Search.Planning.Partitions;
using Domain.TravelRules;

namespace Domain.Search.Composition;

/// <summary>
/// Compose offers based on the collected tickets :
/// - individual tickets as-is
/// - combinations of tickets using each explored partitioning scheme
/// </summary>
/// <param name="airports">The catalog of the airports</param>
/// <param name="connectionPolicy">The connection rules</param>
/// <param name="rates">Exchanges rates to to convert different currencies</param>
public sealed class Composer(AirportCatalog airports, ConnectionPolicy connectionPolicy, ExchangeRates rates)
{
    /// <summary>Build all the possible offers</summary>
    /// <param name="request">The user request</param>
    /// <param name="snapshot">The collected tickets, all in the user currency request</param>
    /// <param name="plans">The explored partitions plan</param>
    /// <param name="alternativesPerSide">
    /// For each anchor, the maximum number of tickets selected from the other side (the cheapest ones that connect).
    /// This limits the number of combinations while still giving the ranking system a choice of schedules.
    /// </param>
    /// <exception cref="ArgumentException">A ticket is not in the search currency.</exception>
    public IReadOnlyList<Offer> Compose(SearchRequest request, IEnumerable<Ticket> snapshot, IEnumerable<PartitionPlan> plans, int alternativesPerSide = 3)
    {
        var tickets = snapshot.ToList();
        if (tickets.FirstOrDefault(t => t.Price.Currency != request.Currency) is { } foreign)
            throw new ArgumentException(
                $"Amount in {foreign.Price.Currency} in a search for {request.Currency} : to be normalized upon ingestion.", nameof(snapshot));

        // Same flight fro many sellers, we keep the cheapest and we list the others
        List<OfferedTicket> offered = [.. tickets
            .Where(t => request.Baggage == BaggageOption.CabinOnly || t.CheckedBagIncluded)
            .GroupBy(t => t.Key)
            .Select(sameFlights =>
            {
                var bySeller = sameFlights.OrderBy(t => t.Price).ToList();
                return new OfferedTicket(bySeller[0], bySeller.Skip(1).ToList());
            })
        ];

        var offers = SingleTickets(request, offered).ToList();
        foreach (PartitionPlan plan in plans)
            offers.AddRange(ViaPartition(request, plan, offered, alternativesPerSide));

        return offers;
    }

    /// <summary>Round-trip travel between the origin and destination, purchased as a single ticket.</summary>
    private IEnumerable<Offer> SingleTickets(SearchRequest request, IReadOnlyList<OfferedTicket> offered) =>
        offered
            .Where(o => o.Selected.ReturnJourney is { } back
                && request.Origins.Contains(o.Selected.FirstJourney.Origin) && o.Selected.FirstJourney.Destination == request.Destination
                && back.Origin == request.Destination && request.Origins.Contains(back.Destination)
                && request.OutboundDates.Contains(o.Selected.FirstJourney.DepartureDate)
                && request.ReturnDates.Contains(back.DepartureDate))
            .Select(o => Build(request, [o], [o.Selected.FirstJourney], [o.Selected.ReturnJourney!], via: null))
            .OfType<Offer>();

    /// <summary>
    /// What one side of the ticket represents in an offer: a round trip, or two one-way trips.
    /// In both cases, one trip in each direction.
    /// </summary>
    private sealed record SideOption(IReadOnlyList<OfferedTicket> Tickets, Journey Outward, Journey Return);

    /// <summary>
    /// Combine the anchor's options with those on the other side that connect to it.
    /// The outbound leg goes from the origin to the destination; the return leg goes in the opposite direction.
    /// </summary>
    private IEnumerable<Offer> ViaPartition(SearchRequest request, PartitionPlan plan, IReadOnlyList<OfferedTicket> offered, int alternativesPerSide)
    {
        var legs = new PartitionLegs(airports, request, plan.Partition);

        foreach (SideOption anchor in SideOptions(request, legs, plan.AnchorSide, offered, alternativesPerSide, connectingTo: null))
            foreach (SideOption other in SideOptions(request, legs, plan.OtherSide, offered, alternativesPerSide, connectingTo: anchor))
            {
                (SideOption? origin, SideOption? destination) = plan.AnchorSide == TripSide.Origin ? (anchor, other) : (other, anchor);
                OfferedTicket[] tickets = [.. origin.Tickets.Concat(destination.Tickets).OrderBy(t => t.Selected.FirstJourney.DepartureAt)];

                Offer? offer = Build(request, tickets, [origin.Outward, destination.Outward], [destination.Return, origin.Return], plan.Partition);
                if (offer is not null)
                    yield return offer;
            }
    }

    /// <summary>
    /// The options on one side.
    /// Without <paramref name="connectingTo"/>, the results are the options for the anchor :
    /// - all round-trips, or the x cheapest one-way trips in each direction
    /// With it, the results are the x cheapest options that connect to this anchor in both directions.
    /// </summary>
    private List<SideOption> SideOptions(
        SearchRequest request,
        PartitionLegs legs,
        TripSide side,
        IReadOnlyList<OfferedTicket> offered,
        int alternativesPerSide,
        SideOption? connectingTo)
    {
        bool Connects(Journey journey, TravelDirection direction) =>
            connectingTo is null || ConnectsTo(connectingTo, journey, side, direction, request.Baggage);

        if (legs.Partition.PricingOf(side) == LegPricing.RoundTrip)
        {
            IOrderedEnumerable<OfferedTicket> roundTrips = offered
                .Where(o => legs.IsRoundTripOn(o.Selected, side)
                    && Connects(o.Selected.FirstJourney, TravelDirection.Outward)
                    && Connects(o.Selected.ReturnJourney!, TravelDirection.Return))
                .OrderBy(o => o.Selected.Price);

            return [..
                (connectingTo is null ? roundTrips : roundTrips.Take(alternativesPerSide))
                .Select(o => new SideOption([o], o.Selected.FirstJourney, o.Selected.ReturnJourney!))
            ];
        }

        List<OfferedTicket> OneWays(TravelDirection direction) => [.. offered
            .Where(o => legs.IsOneWayOn(o.Selected, side, direction) && Connects(o.Selected.FirstJourney, direction))
            .OrderBy(o => o.Selected.Price)
            .Take(alternativesPerSide)
        ];

        return [..
            from outward in OneWays(TravelDirection.Outward)
            from back in OneWays(TravelDirection.Return)
            where back.Selected.FirstJourney.DepartureAt > outward.Selected.FirstJourney.ArrivalAt
            select new SideOption([outward, back], outward.Selected.FirstJourney, back.Selected.FirstJourney)
        ];
    }

    /// <summary>
    /// Does a path in this direction connect to the anchor path in the same direction, in the correct order?
    /// </summary>
    private bool ConnectsTo(SideOption anchor, Journey journey, TripSide side, TravelDirection direction, BaggageOption baggage)
    {
        Journey anchorJourney = direction == TravelDirection.Outward ? anchor.Outward : anchor.Return;
        (Journey? first, Journey? second) = Partition.ComesFirst(side, direction) ? (journey, anchorJourney) : (anchorJourney, journey);
        return connectionPolicy.AllowsSelfTransfer(first.Destination, first.ArrivalAt, second.Origin, second.DepartureAt, baggage);
    }

    private Offer? Build(SearchRequest request, OfferedTicket[] tickets, Journey[] outbound, Journey[] inbound, Partition? via) =>
        inbound[0].DepartureAt <= outbound[^1].ArrivalAt ? null : Assess(request, new Offer(tickets, outbound, inbound, via));

    /// <summary>
    /// Calculate the total cost of the package beyond the tickets and list what needs to be noted.
    /// </summary>
    /// <returns>The completed bid, or <see langword="null"/> if it is rejected.</returns>
    private Offer? Assess(SearchRequest request, Offer offer)
    {
        var costs = new List<AncillaryCost>();
        var warnings = new List<OfferWarning>();

        if (offer.SelfTransfers > 0)
            warnings.Add(new SelfTransferWarning(offer.SelfTransfers));

        foreach (Connection? c in offer.Connections.Where(c => c.ChangesAirport))
        {
            warnings.Add(new AirportChangeWarning(c.Arriving.To, c.Departing.From));
            if (connectionPolicy.TransferBetween(c.Arriving.To, c.Departing.From) is { } transfer)
                costs.Add(Cost(AncillaryKind.GroundTransfer, $"Transfer {transfer.From} → {transfer.To}", transfer.Cost, request));
        }

        foreach (Connection? c in offer.Connections.Where(c => c.Kind == ConnectionKind.SelfTransfer && c.Layover >= connectionPolicy.OvernightThreshold))
        {
            warnings.Add(new OvernightLayoverWarning(c.At, c.Layover));
            costs.Add(Cost(AncillaryKind.HotelNight, $"Night at {c.At}", connectionPolicy.EstimatedHotelNight, request));
        }

        // For later purposes add transit rules with required documents (ESTA, eTA, visa costs) etc..

        return offer with { AncillaryCosts = costs, Warnings = warnings };
    }

    private AncillaryCost Cost(AncillaryKind kind, string label, Money original, SearchRequest r) =>
        new(kind, label, rates.Convert(original, r.Currency), original);
}
