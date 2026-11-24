namespace Domain.Search;

/// <summary>
/// Represents a user's multi-criteria flight search request.
/// </summary>
/// <remarks>
/// <para>
/// The return date is DERIVED, never given: a traveller asks for a stay of so many nights
/// inside a departure window, not for two independent date ranges. Two ranges both
/// under-express the request — they cannot say "exactly nineteen nights" — and over-generate
/// it: a three-day window against a three-day return range admits nine trips where three are
/// wanted. On a budget of a few thousand page loads a day, that factor is the whole problem.
/// </para>
/// <para>
/// <see cref="Candidates"/> is therefore the request's real content, and
/// <see cref="OutboundDates"/> and <see cref="ReturnDates"/> are only their envelopes, kept
/// because the planner still reasons in windows.
/// </para>
/// <para>
/// The sequences are taken as <see cref="IEnumerable{T}"/> and not as sets so that the order
/// of <see cref="Candidates"/> is observable: it drives the order in which pages are loaded
/// and the cache keys that result, so it is part of the contract rather than a detail.
/// </para>
/// </remarks>
public sealed record SearchRequest
{
    /// <summary>The accepted departure airports.</summary>
    public IReadOnlySet<AirportCode> Origins { get; }

    /// <summary>The arrival airport.</summary>
    public AirportCode Destination { get; }

    /// <summary>The dates on which the outbound journey may leave.</summary>
    public DateRange DepartureWindow { get; }

    /// <summary>The accepted stay lengths at the destination, in nights.</summary>
    public IReadOnlySet<int> Nights { get; }

    /// <summary>The weekdays on which the traveller accepts to leave.</summary>
    public IReadOnlySet<DayOfWeek> DepartureDays { get; }

    /// <summary>The weekdays on which the traveller accepts to come back.</summary>
    public IReadOnlySet<DayOfWeek> ReturnDays { get; }

    /// <summary>Cabin only or checked baggage.</summary>
    public BaggageOption Baggage { get; }

    /// <summary>The ISO 4217 currency code the prices are wanted in.</summary>
    public string Currency { get; }

    /// <summary>When the request was made.</summary>
    public DateTimeOffset RequestedAt { get; }

    /// <summary>
    /// Every trip this request asks for, ordered by origin, then departure date, then stay
    /// length. Never empty.
    /// </summary>
    public IReadOnlyList<TripCandidate> Candidates { get; }

    /// <summary>The envelope of the candidate departure dates.</summary>
    /// <remarks>
    /// Narrower than <see cref="DepartureWindow"/> whenever the accepted weekdays exclude its
    /// first or last days. That tightness is not cosmetic: the planner turns this range into
    /// one query per date it contains.
    /// </remarks>
    public DateRange OutboundDates { get; }

    /// <summary>The envelope of the candidate return dates.</summary>
    public DateRange ReturnDates { get; }

    private readonly FrozenSet<(AirportCode Origin, DateOnly Departure, DateOnly? Return)> _admitted;

    /// <param name="origins">The accepted departure airports.</param>
    /// <param name="destination">The arrival airport.</param>
    /// <param name="departureWindow">The dates on which the outbound journey may leave.</param>
    /// <param name="nights">The accepted stay lengths at the destination, in nights.</param>
    /// <param name="departureDays">The weekdays on which the traveller accepts to leave.</param>
    /// <param name="returnDays">The weekdays on which the traveller accepts to come back.</param>
    /// <param name="baggage">Cabin only or checked baggage.</param>
    /// <param name="currency">The ISO 4217 currency code the prices are wanted in.</param>
    /// <param name="requestedAt">When the request was made.</param>
    /// <exception cref="ArgumentException">
    /// There is no origin, the destination is one of the origins, there is no accepted stay
    /// length or one of them is negative, there is no accepted weekday on either side, or the
    /// criteria together admit no trip at all.
    /// </exception>
    public SearchRequest(
        IEnumerable<AirportCode> origins,
        AirportCode destination,
        DateRange departureWindow,
        IEnumerable<int> nights,
        IEnumerable<DayOfWeek> departureDays,
        IEnumerable<DayOfWeek> returnDays,
        BaggageOption baggage,
        string currency,
        DateTimeOffset requestedAt)
    {
        AirportCode[] chosen = [.. origins];
        int[] stays = [.. nights];

        if (chosen.Length == 0)
            throw new ArgumentException("At least one departure airport must be chosen.", nameof(origins));
        if (chosen.Contains(destination))
            throw new ArgumentException("The destination cannot be one of the departure airports.", nameof(destination));
        if (stays.Length == 0)
            throw new ArgumentException(
                "A round trip needs at least one accepted stay length. One-way search is not supported yet.",
                nameof(nights));
        if (stays.Any(stay => stay < 0))
            throw new ArgumentException("A stay length cannot be negative.", nameof(nights));

        Origins = chosen.ToFrozenSet();
        Destination = destination;
        DepartureWindow = departureWindow;
        Nights = stays.ToFrozenSet();
        DepartureDays = departureDays.ToFrozenSet();
        ReturnDays = returnDays.ToFrozenSet();
        (Baggage, Currency, RequestedAt) = (baggage, currency, requestedAt);

        if (DepartureDays.Count == 0)
            throw new ArgumentException("At least one departure weekday must be accepted.", nameof(departureDays));
        if (ReturnDays.Count == 0)
            throw new ArgumentException("At least one return weekday must be accepted.", nameof(returnDays));

        Candidates = Enumerate(chosen, stays);
        if (Candidates.Count == 0)
            throw new ArgumentException(
                "These criteria admit no trip: the accepted weekdays exclude every date of the departure window, "
                + "or every return the accepted stay lengths imply.",
                nameof(departureWindow));

        _admitted = Candidates
            .Select(candidate => (candidate.Origin, candidate.Departure, candidate.Return))
            .ToFrozenSet();
        OutboundDates = Envelope(Candidates.Select(candidate => candidate.Departure));
        ReturnDates = Envelope(Candidates.Select(candidate => candidate.Return!.Value));
    }

    /// <summary>
    /// Whether this request asks for the trip leaving <paramref name="origin"/> on
    /// <paramref name="departure"/> and coming back on <paramref name="back"/>, that is whether
    /// that trip is one of the <see cref="Candidates"/>.
    /// </summary>
    /// <remarks>
    /// Never answer this from the envelopes: with nineteen nights and a departure between the
    /// 1st and the 3rd, leaving on the 3rd and coming back on the 20th sits inside both of them
    /// and is seventeen nights nobody asked for. A predicate, so it never throws, whatever the
    /// trip it is given.
    /// </remarks>
    /// <param name="origin">The airport the outbound journey leaves from.</param>
    /// <param name="departure">The date the outbound journey leaves on.</param>
    /// <param name="back">
    /// The date the return journey leaves the destination on, or <see langword="null"/> for a one-way.
    /// </param>
    public bool Admits(AirportCode origin, DateOnly departure, DateOnly? back) =>
        _admitted.Contains((origin, departure, back));

    /// <summary>
    /// The candidates, in the one order the contract fixes. The origins are sorted ordinally
    /// and the stay lengths ascending, so two requests holding the same criteria enumerate
    /// identically whatever order they were given in.
    /// </summary>
    private TripCandidate[] Enumerate(IEnumerable<AirportCode> chosen, IEnumerable<int> stays) =>
    [
        .. from origin in chosen.Distinct().OrderBy(origin => origin.Value, StringComparer.Ordinal)
           from departure in DaysOf(DepartureWindow)
           where DepartureDays.Contains(departure.DayOfWeek)
           from stay in stays.Distinct().Order()
           let back = departure.AddDays(stay)
           where ReturnDays.Contains(back.DayOfWeek)
           select new TripCandidate(origin, Destination, departure, back)
    ];

    private static IEnumerable<DateOnly> DaysOf(DateRange window)
    {
        for (DateOnly day = window.From; day <= window.To; day = day.AddDays(1))
            yield return day;
    }

    private static DateRange Envelope(IEnumerable<DateOnly> dates)
    {
        DateOnly[] all = [.. dates];
        return new DateRange(all.Min(), all.Max());
    }
}
