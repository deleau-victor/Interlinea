using Domain.Search;

namespace Domain.UnitTests.Search;

/// <remarks>
/// Every date here is in December 2026, so the expectations name days of the month.
/// The 1st is a Tuesday, the 2nd a Wednesday, the 3rd a Thursday; nineteen nights later
/// the 20th is a Sunday, the 21st a Monday, the 22nd a Tuesday.
/// </remarks>
public sealed class SearchRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 0, 0, 0, TimeSpan.Zero);

    private static DayOfWeek[] AllDays => Enum.GetValues<DayOfWeek>();

    private static DateRange FirstThreeOfDecember => new(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3));

    private static SearchRequest Request(
        IEnumerable<int>? nights = null,
        IEnumerable<DayOfWeek>? departureDays = null,
        IEnumerable<DayOfWeek>? returnDays = null,
        IEnumerable<AirportCode>? origins = null,
        DateRange? window = null) => new(
        origins ?? [new AirportCode("CDG")],
        new AirportCode("LIM"),
        window ?? FirstThreeOfDecember,
        nights ?? [19],
        departureDays ?? AllDays,
        returnDays ?? AllDays,
        BaggageOption.CabinOnly,
        "EUR",
        Now);

    /// <summary>The candidates as (origin, departure day, return day), to read expectations at a glance.</summary>
    private static (string Origin, int Departure, int Return)[] Trips(SearchRequest request) =>
        [.. request.Candidates.Select(candidate =>
            (candidate.Origin.Value, candidate.Departure.Day, candidate.Return!.Value.Day))];

    // —————————————————————— CANDIDATE TESTS ——————————————————————

    [Fact]
    public void Candidates_WithThreeDeparturesAndOneStayLength_AreThreeTripsAndNotNine()
    {
        // Arrange & Act — this is the whole point of the shape: a three-day window and a
        // nineteen-night stay ask for THREE trips. Two independent date ranges covering the
        // same dates admitted nine, six of which the traveller never asked for.
        (string, int, int)[] trips = Trips(Request());

        // Assert
        trips.Should().BeEquivalentTo([("CDG", 1, 20), ("CDG", 2, 21), ("CDG", 3, 22)]);
    }

    [Fact]
    public void Candidates_WithTwoStayLengths_AreOnePerDepartureAndPerStayLength()
    {
        // Arrange & Act
        (string, int, int)[] trips = Trips(Request(nights: [19, 20]));

        // Assert
        trips.Should().BeEquivalentTo([
            ("CDG", 1, 20), ("CDG", 1, 21),
            ("CDG", 2, 21), ("CDG", 2, 22),
            ("CDG", 3, 22), ("CDG", 3, 23),
        ]);
    }

    [Fact]
    public void Candidates_WithSeveralOrigins_AreOnePerOrigin()
    {
        // Arrange & Act
        (string, int, int)[] trips = Trips(Request(origins: [new AirportCode("CDG"), new AirportCode("ORY")]));

        // Assert
        trips.Should().BeEquivalentTo([
            ("CDG", 1, 20), ("CDG", 2, 21), ("CDG", 3, 22),
            ("ORY", 1, 20), ("ORY", 2, 21), ("ORY", 3, 22),
        ]);
    }

    [Fact]
    public void Candidates_WithAStayOfZeroNights_ComeBackTheSameDay()
    {
        // Arrange & Act
        (string, int, int)[] trips = Trips(Request(nights: [0]));

        // Assert
        trips.Should().BeEquivalentTo([("CDG", 1, 1), ("CDG", 2, 2), ("CDG", 3, 3)]);
    }

    [Fact]
    public void Candidates_AreOrderedByOriginThenDepartureThenStayLength()
    {
        // Arrange — given in the WRONG order on purpose: Orly before Roissy, twenty nights
        // before nineteen. Taking sequences rather than sets is what makes this observable;
        // from a set, the input order would be the hash order and this test could not tell a
        // sorted enumeration from an unsorted one.
        SearchRequest request = Request(
            nights: [20, 19],
            origins: [new AirportCode("ORY"), new AirportCode("CDG")]);

        // Act
        (string, int, int)[] trips = Trips(request);

        // Assert
        trips.Should().Equal([
            ("CDG", 1, 20), ("CDG", 1, 21),
            ("CDG", 2, 21), ("CDG", 2, 22),
            ("CDG", 3, 22), ("CDG", 3, 23),
            ("ORY", 1, 20), ("ORY", 1, 21),
            ("ORY", 2, 21), ("ORY", 2, 22),
            ("ORY", 3, 22), ("ORY", 3, 23),
        ]);
    }

    // —————————————————————— WEEKDAY TESTS ——————————————————————

    [Fact]
    public void Candidates_WithOnlyOneAcceptedDepartureWeekday_KeepOnlyThatDeparture()
    {
        // Arrange & Act — only Wednesday, and the 2nd is the only Wednesday of the window.
        (string, int, int)[] trips = Trips(Request(departureDays: [DayOfWeek.Wednesday]));

        // Assert
        trips.Should().BeEquivalentTo([("CDG", 2, 21)]);
    }

    [Fact]
    public void Candidates_WithOnlyOneAcceptedReturnWeekday_KeepOnlyThatReturn()
    {
        // Arrange & Act — only Monday. Of the three returns the stay implies, the 21st is the
        // only Monday. The departure that reaches it, the 2nd, is a WEDNESDAY: a filter that
        // read the departure's weekday instead of the return's would keep nothing at all.
        (string, int, int)[] trips = Trips(Request(returnDays: [DayOfWeek.Monday]));

        // Assert
        trips.Should().BeEquivalentTo([("CDG", 2, 21)]);
    }

    // —————————————————————— ENVELOPE TESTS ——————————————————————

    [Fact]
    public void OutboundDates_IsTheEnvelopeOfTheCandidates_NotTheWholeWindow()
    {
        // Arrange & Act — the window spans the 1st to the 3rd, but only the 2nd is accepted.
        SearchRequest request = Request(departureDays: [DayOfWeek.Wednesday]);

        // Assert — the planner turns this range into one query per date it contains, so the
        // difference between the 1st-3rd and the 2nd-2nd is three page loads against one.
        request.DepartureWindow.Should().Be(FirstThreeOfDecember);
        request.OutboundDates.Should().Be(new DateRange(new DateOnly(2026, 12, 2), new DateOnly(2026, 12, 2)));
    }

    [Fact]
    public void ReturnDates_IsTheEnvelopeOfTheCandidateReturns()
    {
        // Arrange & Act — nineteen and twenty nights from the 1st, the 2nd and the 3rd reach
        // the 20th at the earliest and the 23rd at the latest.
        SearchRequest request = Request(nights: [19, 20]);

        // Assert
        request.ReturnDates.Should().Be(new DateRange(new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 23)));
    }

    // —————————————————————— VALIDATION TESTS ——————————————————————

    [Fact]
    public void Constructor_WithoutAnyOrigin_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<SearchRequest> request = () => Request(origins: []);

        // Assert
        request.Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be("origins");
    }

    [Fact]
    public void Constructor_WithTheDestinationAmongTheOrigins_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<SearchRequest> request = () => Request(origins: [new AirportCode("CDG"), new AirportCode("LIM")]);

        // Assert
        request.Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be("destination");
    }

    [Fact]
    public void Constructor_WithoutAnyStayLength_ThrowsArgumentException()
    {
        // Arrange & Act — a one-way request is well formed, it is simply not served yet.
        Func<SearchRequest> request = () => Request(nights: []);

        // Assert
        request.Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be("nights");
    }

    [Fact]
    public void Constructor_WithANegativeStayLength_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<SearchRequest> request = () => Request(nights: [19, -1]);

        // Assert
        request.Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be("nights");
    }

    [Fact]
    public void Constructor_WithoutAnyAcceptedDepartureWeekday_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<SearchRequest> request = () => Request(departureDays: []);

        // Assert
        request.Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be("departureDays");
    }

    [Fact]
    public void Constructor_WithoutAnyAcceptedReturnWeekday_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<SearchRequest> request = () => Request(returnDays: []);

        // Assert
        request.Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be("returnDays");
    }

    [Fact]
    public void Constructor_WithCriteriaThatAdmitNoTrip_ThrowsArgumentException()
    {
        // Arrange & Act — the window holds a Tuesday, a Wednesday and a Thursday, and only
        // Sunday is accepted. The criteria are each valid and together they ask for nothing.
        Func<SearchRequest> request = () => Request(departureDays: [DayOfWeek.Sunday]);

        // Assert
        request.Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be("departureWindow");
    }
}
