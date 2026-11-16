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
    private static Ticket[] MadridProbes(string currency) =>
    [
        Flights.Ticket(110m, currency, Flights.Journey("CDG", "MAD", new DateTimeOffset(2026, 12, 1, 8, 0, 0, TimeSpan.Zero), duration: 2)),
        Flights.Ticket(100m, currency, Flights.Journey("MAD", "CDG", new DateTimeOffset(2026, 12, 20, 18, 0, 0, TimeSpan.Zero), duration: 2)),
        Flights.Ticket(660m, currency,
            Flights.Journey("MAD", "LIM", new DateTimeOffset(2026, 12, 1, 12, 0, 0, TimeSpan.Zero)),
            Flights.Journey("LIM", "MAD", new DateTimeOffset(2026, 12, 20, 2, 0, 0, TimeSpan.Zero))),
    ];

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
}
