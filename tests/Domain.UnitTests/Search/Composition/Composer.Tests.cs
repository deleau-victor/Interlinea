using Domain.Search;
using Domain.Search.Composition;
using Domain.Search.Offers;
using Domain.Search.Planning.Partitions;

namespace Domain.UnitTests.Search.Composition;

/// <remarks>
/// Every date here is in December 2026. The 1st is a Tuesday, the 2nd a Wednesday, the 3rd a
/// Thursday; the 20th is a Sunday, the 21st a Monday, the 22nd a Tuesday, the 23rd a Wednesday.
/// With nineteen nights and a departure between the 1st and the 3rd, the request asks for
/// (1st→20th), (2nd→21st) and (3rd→22nd), and its envelopes are 1st-3rd and 20th-22nd.
/// </remarks>
public sealed class ComposerTests
{
    private static readonly Composer Composer = new(Flights.Airports, Flights.Policy, new ExchangeRates([]));

    private static DateOnly December(int day) => new(2026, 12, day);

    private static DateTimeOffset At(int day, int hour) => new(2026, 12, day, hour, 0, 0, TimeSpan.Zero);

    private static SearchRequest Request(
        IEnumerable<int>? nights = null,
        IEnumerable<DayOfWeek>? returnDays = null,
        IEnumerable<AirportCode>? origins = null) => new(
        origins ?? [new AirportCode("CDG")],
        new AirportCode("LIM"),
        new DateRange(December(1), December(3)),
        nights ?? [19],
        Enum.GetValues<DayOfWeek>(),
        returnDays ?? Enum.GetValues<DayOfWeek>(),
        BaggageOption.CabinOnly,
        "EUR",
        Flights.ObservedAt);

    /// <summary>A round trip to Lima bought as one ticket.</summary>
    private static Ticket RoundTrip(int departureDay, int returnDay, string backTo = "CDG") => Flights.Ticket(
        700m, "EUR",
        Flights.Journey("CDG", "LIM", At(departureDay, 10)),
        Flights.Journey("LIM", backTo, At(returnDay, 10)));

    private static IReadOnlyList<Offer> SingleTickets(SearchRequest request, params Ticket[] tickets) =>
        Composer.Compose(request, tickets, []);

    // —————————————————————— SINGLE TICKET TESTS ——————————————————————

    [Fact]
    public void SingleTicket_ForARequestedTrip_IsOffered()
    {
        // Arrange — nineteen nights from the 1st: the first candidate of the request.
        Ticket ticket = RoundTrip(1, 20);

        // Act
        IReadOnlyList<Offer> offers = SingleTickets(Request(), ticket);

        // Assert
        offers.Should().ContainSingle().Which.Tickets.Should().ContainSingle()
            .Which.Selected.Should().BeSameAs(ticket);
    }

    [Fact]
    public void SingleTicket_WithAStayLengthNotRequested_IsNotOffered()
    {
        // Arrange — the 3rd and the 20th each sit inside their envelope, but seventeen nights
        // were never asked for. Only the stay length refuses this ticket.
        Ticket ticket = RoundTrip(3, 20);

        // Act
        IReadOnlyList<Offer> offers = SingleTickets(Request(), ticket);

        // Assert
        offers.Should().BeEmpty();
    }

    [Fact]
    public void SingleTicket_ReturningOnARefusedWeekday_IsNotOffered()
    {
        // Arrange — nineteen or twenty nights, back on a Monday or a Wednesday: the candidates
        // are (1st→21st), (2nd→21st) and (3rd→23rd), the return envelope 21st-23rd. Leaving on
        // the 2nd and coming back on the 22nd is twenty nights, an accepted stay, and both dates
        // sit inside their envelopes: only the 22nd being a TUESDAY refuses it.
        SearchRequest request = Request(nights: [19, 20], returnDays: [DayOfWeek.Monday, DayOfWeek.Wednesday]);
        Ticket ticket = RoundTrip(2, 22);

        // Act
        IReadOnlyList<Offer> offers = SingleTickets(request, ticket);

        // Assert
        offers.Should().BeEmpty();
    }

    // —————————————————————— RETURN CITY TESTS ——————————————————————

    /// <summary>
    /// Montreal is one of the accepted origins on purpose: a rule that only asks for the
    /// return to land on SOME origin accepts Montreal and refuses Orly, the exact opposite of
    /// the rule wanted.
    /// </summary>
    private static SearchRequest FromParisOrMontreal() =>
        Request(origins: [new AirportCode("CDG"), new AirportCode("YUL")]);

    [Fact]
    public void SingleTicket_ReturningToAnotherCity_IsNotOffered()
    {
        // Arrange — left from Paris, back to Montreal.
        Ticket ticket = RoundTrip(1, 20, backTo: "YUL");

        // Act
        IReadOnlyList<Offer> offers = SingleTickets(FromParisOrMontreal(), ticket);

        // Assert
        offers.Should().BeEmpty();
    }

    [Fact]
    public void SingleTicket_ReturningToAnotherAirportOfTheSameCity_IsOffered()
    {
        // Arrange — left from Roissy, back to Orly: Orly is not an accepted origin, but it is Paris.
        Ticket ticket = RoundTrip(1, 20, backTo: "ORY");

        // Act
        IReadOnlyList<Offer> offers = SingleTickets(FromParisOrMontreal(), ticket);

        // Assert
        offers.Should().ContainSingle().Which.Tickets.Should().ContainSingle()
            .Which.Selected.Should().BeSameAs(ticket);
    }

    [Fact]
    public void SingleTicket_ReturningToAnAirportMissingFromTheCatalog_IsNotOfferedAndDoesNotThrow()
    {
        // Arrange — Brussels is not in the catalog. One unknown airport sent by the worker must
        // cost one ticket, not the whole search.
        Ticket ticket = RoundTrip(1, 20, backTo: "BRU");

        // Act
        IReadOnlyList<Offer> offers = SingleTickets(Request(), ticket);

        // Assert
        offers.Should().BeEmpty();
    }

    [Fact]
    public void SingleTicket_ReturningToTheSameAirportMissingFromTheCatalog_IsOffered()
    {
        // Arrange — Brussels is not in the catalog, but there and back from Brussels needs no
        // catalog to be a trip home.
        SearchRequest request = Request(origins: [new AirportCode("BRU")]);
        Ticket ticket = Flights.Ticket(700m, "EUR",
            Flights.Journey("BRU", "LIM", At(1, 10)),
            Flights.Journey("LIM", "BRU", At(20, 10)));

        // Act
        IReadOnlyList<Offer> offers = SingleTickets(request, ticket);

        // Assert
        offers.Should().ContainSingle().Which.Tickets.Should().ContainSingle()
            .Which.Selected.Should().BeSameAs(ticket);
    }

    // —————————————————————— PARTITION TESTS ——————————————————————

    /// <summary>
    /// One-ways between Paris and Madrid, a Madrid⇄Lima round trip as the anchor. The anchor
    /// leaves Lima on the 20th at 02:00 and lands in Madrid at 14:00, so a departure from Paris
    /// on the 1st is nineteen nights and one on the 3rd is seventeen.
    /// </summary>
    private static (IReadOnlyList<Offer> Offers, Ticket[] Tickets) ViaMadrid(int departureDay) =>
        ViaMadrid(At(departureDay, 8), At(departureDay, 12), At(20, 18));

    private static (IReadOnlyList<Offer> Offers, Ticket[] Tickets) ViaMadrid(
        DateTimeOffset toMadridAt, DateTimeOffset toLimaAt, DateTimeOffset fromMadridAt)
    {
        Ticket toMadrid = Flights.Ticket(80m, "EUR", Flights.Journey("CDG", "MAD", toMadridAt, duration: 2));
        Ticket longHaul = Flights.Ticket(500m, "EUR",
            Flights.Journey("MAD", "LIM", toLimaAt),
            Flights.Journey("LIM", "MAD", At(20, 2)));
        Ticket fromMadrid = Flights.Ticket(90m, "EUR", Flights.Journey("MAD", "CDG", fromMadridAt, duration: 2));

        var plan = new PartitionPlan(
            new Partition(new AirportCode("MAD"), LegPricing.OneWays, LegPricing.RoundTrip),
            TripSide.Destination);

        return (Composer.Compose(Request(), [toMadrid, longHaul, fromMadrid], [plan]), [toMadrid, longHaul, fromMadrid]);
    }

    [Fact]
    public void ViaPartition_ForARequestedTrip_IsOffered()
    {
        // Arrange & Act
        (IReadOnlyList<Offer> offers, Ticket[] tickets) = ViaMadrid(departureDay: 1);

        // Assert
        offers.Should().ContainSingle().Which.Tickets.Select(t => t.Selected).Should().Equal(tickets);
    }

    [Fact]
    public void ViaPartition_WithAStayLengthNotRequested_IsNotOffered()
    {
        // Arrange & Act — every leg sits inside the windows the partition searches, which are
        // the request's envelopes widened by one day. Only the seventeen nights refuse it.
        (IReadOnlyList<Offer> offers, _) = ViaMadrid(departureDay: 3);

        // Assert
        offers.Should().BeEmpty();
    }

    [Fact]
    public void ViaPartition_WithAnOutboundConnectionPastMidnight_IsDatedByTheDepartureFromTheOrigin()
    {
        // Arrange & Act — Paris on the 1st at 22:00, Madrid at midnight, Lima on the 2nd at
        // 03:00. Dated by the departure from Paris this is the requested (1st→20th); dated by
        // the long-haul it would be eighteen nights, never asked for.
        (IReadOnlyList<Offer> offers, Ticket[] tickets) = ViaMadrid(At(1, 22), At(2, 3), At(20, 18));

        // Assert
        offers.Should().ContainSingle().Which.Tickets.Select(t => t.Selected).Should().Equal(tickets);
    }

    [Fact]
    public void ViaPartition_WithAnOvernightConnectionOnTheWayBack_IsDatedByTheDepartureFromTheDestination()
    {
        // Arrange & Act — Lima on the 20th, a night in Madrid, Paris on the 21st. Dated by the
        // day the return leaves Lima this is the requested (1st→20th); dated by the last
        // flight it would be twenty nights, never asked for.
        (IReadOnlyList<Offer> offers, Ticket[] tickets) = ViaMadrid(At(1, 8), At(1, 12), At(21, 8));

        // Assert
        offers.Should().ContainSingle().Which.Tickets.Select(t => t.Selected).Should().Equal(tickets);
    }
}
