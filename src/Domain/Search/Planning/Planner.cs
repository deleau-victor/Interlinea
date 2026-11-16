using Domain.Fares;
using Domain.Reference.Airports;
using Domain.Search.Planning.Hubs;
using Domain.Search.Planning.Partitions;
using Domain.TravelRules;

namespace Domain.Search.Planning;

public sealed class Planner
{
    private readonly AirportCatalog _airports;
    private readonly ConnectionPolicy _connections;
    private readonly ProbePolicy _probes;
    private readonly TimeSpan _hubProfileTtl;
    private readonly ExchangeRates _rates;
    private readonly string _pivotCurrency;

    /// <param name="airports">The airport catalog.</param>
    /// <param name="connections">The self-transfer rules.</param>
    /// <param name="probes">The thresholds deciding which pricing modes a side is probed in.</param>
    /// <param name="hubProfileTtl">How long a hub profile stays usable.</param>
    /// <param name="rates">The exchange rates, used to enter and leave the pivot currency.</param>
    /// <param name="pivotCurrency">The ISO 4217 currency hub profiles are stored in.</param>
    /// <exception cref="ArgumentException"><paramref name="pivotCurrency"/> is not an ISO 4217 code.</exception>
    public Planner(
        AirportCatalog airports,
        ConnectionPolicy connections,
        ProbePolicy probes,
        TimeSpan hubProfileTtl,
        ExchangeRates rates,
        string pivotCurrency)
    {
        _airports = airports;
        _connections = connections;
        _probes = probes;
        _hubProfileTtl = hubProfileTtl;
        _rates = rates;
        _pivotCurrency = new Money(0m, pivotCurrency).Currency;
    }

    public static IReadOnlyList<TripSide> Sides { get; } =
        Array.AsReadOnly([TripSide.Origin, TripSide.Destination]);

    public static IReadOnlyList<PriceQuery> DirectQueries(SearchRequest r) => r.Origins
        .Select(o => new PriceQuery(new Route(o, r.Destination), TripType.RoundTrip, r.OutboundDates, r.ReturnDates, r.Baggage))
        .ToList();

    #region  ——————————— Hub Discovery ———————————

    /// <summary>
    /// Filter the candidates hubs by their cached profiles
    /// </summary>
    /// <param name="request">The user request</param>
    /// <param name="potentialHubs">The proposed hubs, connected to the destination</param>
    /// <param name="cachedProfiles">The cached profiles hubs</param>
    /// <param name="bestKnownPrice">The best known price to connect the hub</param>
    /// <returns>
    /// - The plans which their floor is under the best offer (retained)
    /// - The plans without cached profile (toProbe)
    /// - The plans which can't be used to make a better offer than the best known offer (pruned)
    /// </returns>
    public PartitionSelection SelectPartitions(
        SearchRequest request,
        IEnumerable<AirportCode> potentialHubs,
        IEnumerable<HubProfile> cachedProfiles,
        Money? bestKnownPrice)
    {
        string originCities = OriginCityKey.Of(_airports, request.Origins);
        var fresh = cachedProfiles
            .Where(p =>
                p.Destination == request.Destination
                && p.OriginCities == originCities
                && request.RequestedAt - p.ProbedAt <= _hubProfileTtl)
            .GroupBy(p => p.Hub)
            .ToDictionary(g => g.Key, g => g.MaxBy(p => p.ProbedAt)!);

        (List<PartitionEstimate>? retained, List<AirportCode>? toProbe, List<PartitionEstimate>? pruned) = (new List<PartitionEstimate>(), new List<AirportCode>(), new List<PartitionEstimate>());

        foreach (AirportCode hub in potentialHubs.Distinct())
        {
            // If we don't have the profile of the hub in our cache, we need to probe it
            if (!fresh.TryGetValue(hub, out HubProfile? profile))
            {
                toProbe.Add(hub);
                continue;
            }

            foreach (PartitionEstimate estimate in profile.Candidates(_rates, request.Currency))
            {
                if (estimate.Floor >= bestKnownPrice)
                    pruned.Add(estimate);
                else
                    retained.Add(estimate);
            }
        }

        return new PartitionSelection(retained.OrderBy(e => e.Floor).ToList(), toProbe, pruned);
    }

    /// <summary>
    /// Build a Hub profile from the prices sighted by <see cref="ProbeQueries"/> for each side :
    /// - The cheapest RT
    /// - The cheapest pair of OW
    /// - And the number of distinct flight each day
    /// </summary>
    /// <param name="request">The user request</param>
    /// <param name="hub">The airport from we are building the profile</param>
    /// <param name="probeResults">The prices sighted while probing. Sightings, not tickets: a probe reads listed prices and only serves to measure a floor</param>
    /// <returns>The profile of the hub</returns>
    public HubProfile Profile(SearchRequest request, AirportCode hub, IEnumerable<PriceSighting> probeResults)
    {
        var legs = new PartitionLegs(_airports, request, new Partition(hub, LegPricing.OneWays, LegPricing.OneWays));
        var sightings = probeResults.ToList();

        LegProfile Measure(TripSide side)
        {
            var outwardOneWays = sightings.Where(s => legs.IsOneWayOn(s, side, TravelDirection.Outward)).ToList();
            var returnOneWays = sightings.Where(s => legs.IsOneWayOn(s, side, TravelDirection.Return)).ToList();
            var roundTrips = sightings.Where(s => legs.IsRoundTripOn(s, side)).ToList();

            Money? oneWays = Cheapest(outwardOneWays) is { } outward && Cheapest(returnOneWays) is { } back
                ? outward + back
                : null;

            int distinctJourneys = outwardOneWays.Concat(returnOneWays).Concat(roundTrips)
                .SelectMany(s => s.Journeys)
                .DistinctBy(j => j.Key)
                .Count();
            int days = legs.Window(side, TravelDirection.Outward).DayCount + legs.Window(side, TravelDirection.Return).DayCount;

            return new LegProfile(ToPivot(oneWays), ToPivot(Cheapest(roundTrips)), (double)distinctJourneys / days);
        }

        return new HubProfile(hub, legs.OriginCityCodes, request.Destination, Measure(TripSide.Origin), Measure(TripSide.Destination), request.RequestedAt);
    }

    /// <summary>
    /// The research to do, para probe a hub
    /// </summary>
    /// <param name="request">The user request</param>
    /// <param name="hub">The airport we want to probe</param>
    /// <returns>The queries to execute to probe the hub</returns>
    public IReadOnlyList<PriceQuery> ProbeQueries(SearchRequest request, AirportCode hub)
    {
        PartitionLegs legs = new(_airports, request, new Partition(hub, LegPricing.OneWays, LegPricing.OneWays));
        List<PriceQuery> queries = [];

        foreach (TripSide side in Sides)
        {
            double distance = legs.Routes(side, TravelDirection.Outward).Max(route => _airports.DistanceKm(route.From, route.To));
            foreach (LegPricing pricing in _probes.ModesFor(distance))
                queries.AddRange(SideQueries(request, legs, side, pricing));
        }

        return Merge(queries);
    }

    #endregion

    #region  ——————————— Phase 1 : Anchoring ———————————

    /// <summary>
    /// In the first phase of researching a result, we need to find an anchor for the plan
    /// </summary>
    /// <param name="request">The user request</param>
    /// <param name="plan">The explored partition</param>
    /// <returns>The queries to execute in phase 1</returns>
    public IReadOnlyList<PriceQuery> AnchorQueries(SearchRequest request, PartitionPlan plan)
    {
        PartitionLegs legs = new(_airports, request, plan.Partition);
        return Merge(SideQueries(request, legs, plan.AnchorSide, plan.Partition.PricingOf(plan.AnchorSide)));
    }

    #endregion

    #region ——————————— Phase 2 : Try to build around the anchor the same day ———————————

    /// <summary>
    /// For each founded anchor, try to build an other segment around the anchor the same day
    ///
    /// CASE 1 : build OW around the anchor
    /// For example, if we want to go from CDG to LIM, and if the anchor is a MAD<->LIM with :
    /// - A departure from Madrid at 11:00 the 10 of the month
    /// - A return at Madrid at 15:30 the 20 of the month
    /// we will try to find a flight from CDG which arrive at MAD 2 or 4 hours before the departure (depends on the policy)
    /// and for the return an return flight which fit the same constraint but to get back at CDG.
    ///
    /// Case 2 : build another RT around the anchor
    /// For example, if we want to go from CDG to LIM, and if the anchor is a YUL<->LIM
    /// we will try to find an RT from CDG to YUL which fit the constraints of the other RT.
    /// </summary>
    /// <param name="request">The user request</param>
    /// <param name="plan">The explored partition and it's anchor</param>
    /// <param name="fetched">The collected ticket, at least the tickets from the first phase</param>
    /// <param name="bestKnownPrice">The best known offer</param>
    /// <returns>The queries to execute</returns>
    public IReadOnlyList<PriceQuery> PositioningQueries(SearchRequest request, PartitionPlan plan, IEnumerable<Ticket> fetched, Money? bestKnownPrice)
    {
        PartitionLegs legs = new(_airports, request, plan.Partition);
        AnchorSet anchors = AnchorsOf(legs, plan, fetched, bestKnownPrice);
        TripSide other = plan.OtherSide;
        var queries = new List<PriceQuery>();

        if (plan.Partition.PricingOf(other) == LegPricing.OneWays)
        {
            foreach (TravelDirection direction in PartitionLegs.Directions)
                foreach (Journey anchor in anchors.In(direction))
                {
                    Bound bound = BoundOf(anchor, Partition.ComesFirst(other, direction), request.Baggage);
                    if (!legs.Window(other, direction).Contains(bound.Date))
                        continue;

                    queries.AddRange(legs.Routes(other, direction, bound.HubAirport)
                        .Select(route => WithOutwardBound(OneWay(route, bound.Date, request.Baggage), bound)));
                }
        }
        else
        {
            foreach ((Journey? outward, Journey? back) in anchors.Pairs)
            {
                Bound outBound = BoundOf(outward, Partition.ComesFirst(other, TravelDirection.Outward), request.Baggage);
                Bound backBound = BoundOf(back, Partition.ComesFirst(other, TravelDirection.Return), request.Baggage);
                if (!legs.Window(other, TravelDirection.Outward).Contains(outBound.Date)
                    || !legs.Window(other, TravelDirection.Return).Contains(backBound.Date))
                    continue;

                queries.AddRange(legs.Routes(other, TravelDirection.Outward, outBound.HubAirport)
                    .Select(route => WithReturnBound(WithOutwardBound(RoundTrip(route, outBound.Date, backBound.Date, request.Baggage), outBound), backBound)));
            }
        }

        return Merge(queries);
    }

    #endregion

    #region ——————————— Phase 2 : Try to build around the anchor the day before or the day after (depending the side) ———————————

    /// <summary>
    /// For each anchors that the phase 2 can't connect, we will try to connect it from the day before, or the next day (depending the side)
    /// </summary>
    /// <param name="request">The user request</param>
    /// <param name="plan">The explored partition and it's anchor</param>
    /// <param name="fetched">The collected ticket, at least the tickets from the two first phases</param>
    /// <param name="bestKnownPrice">The best known offer</param>
    /// <returns>The queries to execute</returns>
    public IReadOnlyList<PriceQuery> OvernightQueries(SearchRequest request, PartitionPlan plan, IEnumerable<Ticket> fetched, Money? bestKnownPrice)
    {
        List<Ticket> tickets = [.. fetched];
        PartitionLegs legs = new(_airports, request, plan.Partition);
        AnchorSet anchors = AnchorsOf(legs, plan, tickets, bestKnownPrice);
        TripSide other = plan.OtherSide;
        var queries = new List<PriceQuery>();

        if (plan.Partition.PricingOf(other) == LegPricing.OneWays)
        {
            foreach (TravelDirection direction in PartitionLegs.Directions)
                foreach (Journey anchor in anchors.In(direction))
                {
                    Bound bound = BoundOf(anchor, Partition.ComesFirst(other, direction), request.Baggage);
                    bool connected = tickets
                        .Where(t => legs.IsOneWayOn(t, other, direction))
                        .Any(t => Connects(t.FirstJourney, anchor, bound, request.Baggage));
                    if (connected || !legs.Window(other, direction).Contains(bound.AdjacentDate))
                        continue;

                    queries.AddRange(legs.Routes(other, direction, bound.HubAirport)
                        .Select(route => OneWay(route, bound.AdjacentDate, request.Baggage)));
                }
        }
        else
        {
            var candidates = tickets.Where(t => legs.IsRoundTripOn(t, other)).ToList();
            foreach ((Journey? outward, Journey? back) in anchors.Pairs)
            {
                Bound outBound = BoundOf(outward, Partition.ComesFirst(other, TravelDirection.Outward), request.Baggage);
                Bound backBound = BoundOf(back, Partition.ComesFirst(other, TravelDirection.Return), request.Baggage);
                bool OutwardOk(Ticket t) => Connects(t.FirstJourney, outward, outBound, request.Baggage);
                bool ReturnOk(Ticket t) => Connects(t.ReturnJourney!, back, backBound, request.Baggage);

                if (candidates.Any(t => OutwardOk(t) && ReturnOk(t)))
                    continue;

                (bool outwardOk, bool returnOk) = (candidates.Any(OutwardOk), candidates.Any(ReturnOk));
                if (outwardOk && returnOk)
                    continue;

                DateOnly outDate = outwardOk ? outBound.Date : outBound.AdjacentDate;
                DateOnly backDate = returnOk ? backBound.Date : backBound.AdjacentDate;
                if (!legs.Window(other, TravelDirection.Outward).Contains(outDate)
                    || !legs.Window(other, TravelDirection.Return).Contains(backDate))
                    continue;

                queries.AddRange(legs.Routes(other, TravelDirection.Outward, outBound.HubAirport).Select(route =>
                {
                    PriceQuery query = RoundTrip(route, outDate, backDate, request.Baggage);
                    if (outwardOk)
                        query = WithOutwardBound(query, outBound);
                    if (returnOk)
                        query = WithReturnBound(query, backBound);
                    return query;
                }));
            }
        }

        return Merge(queries);
    }

    #endregion

    #region ——————————— Internal mechanics ———————————

    private sealed record AnchorSet(
        IReadOnlyList<Journey> Outward,
        IReadOnlyList<Journey> Return,
        IReadOnlyList<(Journey Outward, Journey Return)> Pairs)
    {
        public IReadOnlyList<Journey> In(TravelDirection direction) => direction == TravelDirection.Outward ? Outward : Return;
    }

    private readonly record struct Bound(DateTimeOffset Limit, bool LegPrecedes, AirportCode HubAirport)
    {
        /// <summary>Local day of the deadline, at the hub.</summary>
        public DateOnly Date => DateOnly.FromDateTime(Limit.DateTime);

        /// <summary>Local time of the deadline, at the hub.</summary>
        public TimeOnly Time => TimeOnly.FromDateTime(Limit.DateTime);

        /// <summary>The day before, if the other side goes first; otherwise, the next day.</summary>
        public DateOnly AdjacentDate => LegPrecedes ? Date.AddDays(-1) : Date.AddDays(1);
    }

    private static AnchorSet AnchorsOf(PartitionLegs legs, PartitionPlan plan, IEnumerable<Ticket> fetched, Money? bestKnownPrice)
    {
        TripSide side = plan.AnchorSide;
        List<Ticket>? affordable = [.. fetched.Where(t => bestKnownPrice is not { } best || t.Price < best)];

        if (legs.Partition.PricingOf(side) == LegPricing.RoundTrip)
        {
            var pairs = affordable
                .Where(t => legs.IsRoundTripOn(t, side))
                .Select(t => (Outward: t.FirstJourney, Return: t.ReturnJourney!))
                .DistinctBy(pair => (pair.Outward.Key, pair.Return.Key))
                .ToList();
            return new AnchorSet(
                pairs.Select(pair => pair.Outward).DistinctBy(j => j.Key).ToList(),
                pairs.Select(pair => pair.Return).DistinctBy(j => j.Key).ToList(),
                pairs);
        }

        IReadOnlyList<Journey> OneWays(TravelDirection direction) => affordable
            .Where(t => legs.IsOneWayOn(t, side, direction))
            .Select(t => t.FirstJourney)
            .DistinctBy(j => j.Key)
            .ToList();

        IReadOnlyList<Journey> outwardJourneys = OneWays(TravelDirection.Outward);
        IReadOnlyList<Journey> returnJourneys = OneWays(TravelDirection.Return);
        var allPairs = (from outward in outwardJourneys
                        from back in returnJourneys
                        where back.DepartureAt > outward.ArrivalAt
                        select (outward, back)).ToList();

        return new AnchorSet(outwardJourneys, returnJourneys, allPairs);
    }

    private Bound BoundOf(Journey anchor, bool legPrecedes, BaggageOption baggage)
    {
        AirportCode hub = legPrecedes ? anchor.Origin : anchor.Destination;
        TimeSpan margin = _connections.MinimumSelfTransfer(hub, hub, baggage)!.Value;
        return new Bound(legPrecedes ? anchor.DepartureAt - margin : anchor.ArrivalAt + margin, legPrecedes, hub);
    }

    private bool Connects(Journey candidate, Journey anchor, Bound bound, BaggageOption baggage) =>
    bound.LegPrecedes
        ? _connections.AllowsSelfTransfer(candidate.Destination, candidate.ArrivalAt, anchor.Origin, anchor.DepartureAt, baggage)
        : _connections.AllowsSelfTransfer(anchor.Destination, anchor.ArrivalAt, candidate.Origin, candidate.DepartureAt, baggage);

    private static IEnumerable<PriceQuery> SideQueries(SearchRequest r, PartitionLegs legs, TripSide side, LegPricing pricing)
    {
        IReadOnlyList<Route> outward = legs.Routes(side, TravelDirection.Outward);
        IReadOnlyList<Route> back = legs.Routes(side, TravelDirection.Return);
        (DateRange? outDates, DateRange? backDates) = (legs.Window(side, TravelDirection.Outward), legs.Window(side, TravelDirection.Return));

        return pricing == LegPricing.RoundTrip
            ? outward.Select(route => new PriceQuery(route, TripType.RoundTrip, outDates, backDates, r.Baggage))
            : outward.Select(route => new PriceQuery(route, TripType.OneWay, outDates, null, r.Baggage))
                .Concat(back.Select(route => new PriceQuery(route, TripType.OneWay, backDates, null, r.Baggage)));
    }

    private static PriceQuery OneWay(Route route, DateOnly date, BaggageOption baggage) =>
        new(route, TripType.OneWay, DateRange.Unique(date), null, baggage);

    private static PriceQuery RoundTrip(Route route, DateOnly outward, DateOnly back, BaggageOption baggage) =>
        new(route, TripType.RoundTrip, DateRange.Unique(outward), DateRange.Unique(back), baggage);

    private static PriceQuery WithOutwardBound(PriceQuery query, Bound bound) =>
        bound.LegPrecedes ? query with { ArriveBefore = bound.Time } : query with { DepartAfter = bound.Time };

    private static PriceQuery WithReturnBound(PriceQuery query, Bound bound) =>
        bound.LegPrecedes ? query with { ReturnArriveBefore = bound.Time } : query with { ReturnDepartAfter = bound.Time };

    private static Money? Cheapest(IEnumerable<PriceSighting> sightings) => sightings.Min(s => (Money?)s.Price);

    /// <summary>The same amount in the pivot currency hub profiles are stored in.</summary>
    private Money? ToPivot(Money? amount) => amount is { } measured ? _rates.Convert(measured, _pivotCurrency) : null;

    private static TimeOnly? Earliest(IEnumerable<TimeOnly?> times)
    {
        var all = times.ToList();
        return all.Any(t => t is null) ? null : all.Min();
    }

    private static TimeOnly? Latest(IEnumerable<TimeOnly?> times)
    {
        var all = times.ToList();
        return all.Any(t => t is null) ? null : all.Max();
    }

    private static List<PriceQuery> Merge(IEnumerable<PriceQuery> queries)
    {
        return [.. queries
        .GroupBy(q => (q.Route, q.TripType, q.Departure, q.Return, q.Baggage))
        .Select(g => g.First() with
        {
            DepartAfter = Earliest(g.Select(q => q.DepartAfter)),
            ArriveBefore = Latest(g.Select(q => q.ArriveBefore)),
            ReturnDepartAfter = Earliest(g.Select(q => q.ReturnDepartAfter)),
            ReturnArriveBefore = Latest(g.Select(q => q.ReturnArriveBefore)),
        })];
    }

    #endregion
}
