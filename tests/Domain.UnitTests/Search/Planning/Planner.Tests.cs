using Domain.Search;
using Domain.Search.Planning;
using Domain.Search.Planning.Hubs;
using Domain.Search.Planning.Partitions;

namespace Domain.UnitTests.Search.Planning;

public sealed class PlannerTests
{
    private const string Pivot = "EUR";
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Yesterday = Now.AddDays(-1);
    private static ExchangeRates Rates => new([("EUR", "USD", 1.10m)]);

    private static Planner NewPlanner() => new(
        Flights.Airports, Flights.Policy, new ProbePolicy(2000, 4000), TimeSpan.FromDays(30), Rates, Pivot);

    private static SearchRequest Request(string currency, params string[] origins) => new(
        origins.Select(origin => new AirportCode(origin)).ToHashSet(),
        new AirportCode("LIM"),
        new DateRange(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3)),
        new DateRange(new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 22)),
        BaggageOption.CabinOnly,
        currency,
        Now);

    /// <summary>A profile for MAD held in the pivot currency: 210 of one-ways, then a 620 round trip.</summary>
    private static HubProfile MadridProfile() => new(
        new AirportCode("MAD"),
        new HashSet<CityCode> { new("PAR") },
        new AirportCode("LIM"),
        new LegProfile(new Money(210m, Pivot), null, FlightsPerDay: 4.0),
        new LegProfile(null, new Money(620m, Pivot), FlightsPerDay: 1.0),
        Yesterday);

    /// <summary>
    /// What the cheap sweep brings back on both sides of MAD, in the search currency:
    /// two one-ways on the feeder, one round trip on the long haul.
    /// </summary>
    private static PriceSighting[] MadridProbes(string currency) =>
    [
        Sighting(110m, currency, Flights.Journey("CDG", "MAD", Day(1, 8), duration: 2)),
        Sighting(100m, currency, Flights.Journey("MAD", "CDG", Day(20, 18), duration: 2)),
        Sighting(660m, currency,
            Flights.Journey("MAD", "LIM", Day(1, 12)),
            Flights.Journey("LIM", "MAD", Day(20, 2))),
    ];

    private static PriceSighting Sighting(decimal price, string currency, params Journey[] journeys) =>
        new(journeys, new Money(price, currency));

    private static DateTimeOffset Day(int day, int hour) =>
        new(2026, 12, day, hour, 0, 0, TimeSpan.Zero);

    private static SearchRequest RequestTo(string destination, params string[] origins) => new(
        origins.Select(origin => new AirportCode(origin)).ToHashSet(),
        new AirportCode(destination),
        new DateRange(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3)),
        new DateRange(new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 22)),
        BaggageOption.CabinOnly,
        Pivot,
        Now);

    // —————————————————————— PIVOT CURRENCY TESTS ——————————————————————

    [Fact]
    public void SelectPartitions_WithAProfileCachedInThePivotCurrency_ReturnsItsFloorInTheSearchCurrency()
    {
        // Arrange & Act
        PartitionSelection selection = NewPlanner().SelectPartitions(
            Request("USD", "CDG"), [new AirportCode("MAD")], [MadridProfile()], new Money(1100m, "USD"));

        // Assert
        selection.Retained.Should().ContainSingle()
                 .Which.Floor.Should().Be(new Money(913m, "USD"));
    }

    [Fact]
    public void SelectPartitions_WithAFloorAboveTheBestKnownPriceInAnotherCurrency_PrunesTheCandidate()
    {
        // Arrange & Act
        PartitionSelection selection = NewPlanner().SelectPartitions(
            Request("USD", "CDG"), [new AirportCode("MAD")], [MadridProfile()], new Money(900m, "USD"));

        // Assert
        selection.Retained.Should().BeEmpty();
        selection.Pruned.Should().ContainSingle();
    }

    [Fact]
    public void Profile_WithProbesInAnotherCurrency_StoresItsFloorsInThePivotCurrency()
    {
        // Arrange & Act
        HubProfile profile = NewPlanner().Profile(
            Request("USD", "CDG"), new AirportCode("MAD"), MadridProbes("USD"));

        // Assert
        profile.Currency.Should().Be(Pivot);
        profile.Candidates(Rates, Pivot).Should().ContainSingle()
               .Which.Floor.Should().Be(new Money(790.91m, Pivot));
    }

    // —————————————————————— ONE-WAYS AND ROUND TRIPS TESTS ——————————————————————

    [Fact]
    public void Profile_WithARoundTripOnASideAlsoProbedAsOneWays_DoesNotCountItAmongTheOneWays()
    {
        // Arrange
        // A side whose distance falls between the two probe thresholds is swept in BOTH modes, so the sweep brings back one-ways and round trips on the same side.
        // On a long haul the round trip is the cheaper of the two: counted among the one-ways it would become their floor, and the two modes would stop being distinguishable.
        PriceSighting[] probes =
        [
            // Origin side, one-ways only: 110 + 100 = 210.
            Sighting(110m, Pivot, Flights.Journey("CDG", "MAD", Day(1, 8), duration: 2)),
            Sighting(100m, Pivot, Flights.Journey("MAD", "CDG", Day(20, 18), duration: 2)),

            // Destination side as two one-ways: 700 + 100 = 800.
            Sighting(700m, Pivot, Flights.Journey("MAD", "LIM", Day(1, 12))),
            Sighting(100m, Pivot, Flights.Journey("LIM", "MAD", Day(20, 2))),

            // The same destination side as one round trip, on other flights: 620, cheaper.
            Sighting(620m, Pivot,
                Flights.Journey("MAD", "LIM", Day(1, 14)),
                Flights.Journey("LIM", "MAD", Day(20, 4))),
        ];

        // Act
        HubProfile profile = NewPlanner().Profile(Request(Pivot, "CDG"), new AirportCode("MAD"), probes);

        // Assert
        // 210 + 620 for the round trip, 210 + 800 for the pair of one-ways.
        // Were the round trip read as a one-way, the one-ways floor would be 720, not 800.
        profile.Candidates(Rates, Pivot).Select(candidate => candidate.Floor)
               .Should().Equal([new Money(830m, Pivot), new Money(1010m, Pivot)]);
    }

    // —————————————————————— ORIGIN CITY KEY TESTS ——————————————————————

    [Fact]
    public void SelectPartitions_WithAProfileProbedFromOneAirportOfACity_ServesASearchOnTheWholeCity()
    {
        // Arrange & Act — the profile was probed for PAR; the search accepts CDG and ORY,
        // which are both Paris, so the cached profile matches and nothing is re-probed.
        PartitionSelection selection = NewPlanner().SelectPartitions(
            Request(Pivot, "CDG", "ORY"), [new AirportCode("MAD")], [MadridProfile()], new Money(1100m, Pivot));

        // Assert
        selection.Retained.Should().ContainSingle();
        selection.ToProbe.Should().BeEmpty();
    }

    [Fact]
    public void SelectPartitions_WithAProfileProbedFromAnotherCity_AsksToProbeTheHubAgain()
    {
        // Arrange — a profile probed from Madrid itself, read by a search leaving from Paris.
        HubProfile fromMadrid = new(
            new AirportCode("MAD"), new HashSet<CityCode> { new("MAD") }, new AirportCode("LIM"),
            new LegProfile(new Money(210m, Pivot), null, FlightsPerDay: 4.0),
            new LegProfile(null, new Money(620m, Pivot), FlightsPerDay: 1.0),
            Yesterday);

        // Act
        PartitionSelection selection = NewPlanner().SelectPartitions(
            Request(Pivot, "CDG"), [new AirportCode("MAD")], [fromMadrid], new Money(1100m, Pivot));

        // Assert
        selection.Retained.Should().BeEmpty();
        selection.ToProbe.Should().Equal([new AirportCode("MAD")]);
    }

    [Fact]
    public void SelectPartitions_WithADestinationMissingFromTheAirportCatalog_DoesNotNeedIt()
    {
        // Arrange — the origin city key is computed from the ORIGINS alone. Obtaining it by
        // building a partition hubbed on the destination made this call depend on a destination
        // the catalog may not know.
        SearchRequest request = RequestTo("BOG", "CDG");

        // Act
        Func<PartitionSelection> selection = () => NewPlanner().SelectPartitions(request, [], [], null);

        // Assert
        selection.Should().NotThrow();
    }
}
