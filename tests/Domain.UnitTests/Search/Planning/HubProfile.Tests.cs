using Domain.Search.Planning.Hubs;
using Domain.Search.Planning.Partitions;

namespace Domain.UnitTests.Search.Planning;

public sealed class HubProfileTests
{
    private static readonly DateTimeOffset ProbedAt = new(2026, 11, 15, 0, 0, 0, TimeSpan.Zero);

    private static ExchangeRates NoConversion => new([]);

    private static HubProfile ProfileOf(LegProfile origin, LegProfile destination) => new(
        new AirportCode("MAD"), new HashSet<CityCode> { new("PAR") }, new AirportCode("LIM"),
        origin, destination, ProbedAt);

    private static LegProfile Side(decimal oneWaysFloor, double flightsPerDay, string currency = "EUR") =>
        new(new Money(oneWaysFloor, currency), null, flightsPerDay);

    // —————————————————————— ANCHOR SIDE TESTS ——————————————————————

    [Fact]
    public void Candidates_WhenTheOriginSideHasFewerFlights_AnchorsOnTheOrigin()
    {
        // Arrange
        // One flight a day at the origin against four at the destination.
        // The origin is also the CHEAPER side, so a rule that anchored on the price could not pass here.
        HubProfile profile = ProfileOf(Side(200m, flightsPerDay: 1.0), Side(300m, flightsPerDay: 4.0));

        // Act
        IReadOnlyList<PartitionEstimate> candidates = profile.Candidates(NoConversion, "EUR");

        // Assert
        candidates.Should().ContainSingle()
                  .Which.Plan.AnchorSide.Should().Be(TripSide.Origin);
    }

    [Fact]
    public void Candidates_WhenTheDestinationSideHasFewerFlights_AnchorsOnTheDestination()
    {
        // Arrange
        // The mirror of the case above. Without it, a rule that always answered
        // Origin would satisfy every other test in this file.
        HubProfile profile = ProfileOf(Side(200m, flightsPerDay: 4.0), Side(300m, flightsPerDay: 1.0));

        // Act
        IReadOnlyList<PartitionEstimate> candidates = profile.Candidates(NoConversion, "EUR");

        // Assert
        candidates.Should().ContainSingle()
                  .Which.Plan.AnchorSide.Should().Be(TripSide.Destination);
    }

    [Fact]
    public void Candidates_AtEqualFrequency_AnchorsOnTheMoreExpensiveSide()
    {
        // Arrange
        // Two journeys over six days against one over three: the same frequency, reached by two different divisions, so the decision falls through to the price.
        // This is also where the frequency invariant is exercised: were the two ratios not bit-identical, the tie-break would never be reached and this test would fail.
        HubProfile profile = ProfileOf(Side(300m, flightsPerDay: 2.0 / 6.0), Side(200m, flightsPerDay: 1.0 / 3.0));

        // Act
        IReadOnlyList<PartitionEstimate> candidates = profile.Candidates(NoConversion, "EUR");

        // Assert
        candidates.Should().ContainSingle()
                  .Which.Plan.AnchorSide.Should().Be(TripSide.Origin);
    }

    // —————————————————————— MEASUREMENT TESTS ——————————————————————

    [Fact]
    public void Candidates_WithASideThatWasNeverMeasured_IsEmpty()
    {
        // Arrange
        HubProfile profile = ProfileOf(
            Side(200m, flightsPerDay: 1.0),
            new LegProfile(null, null, FlightsPerDay: 0.0));

        // Act & Assert
        profile.Candidates(NoConversion, "EUR").Should().BeEmpty();
    }

    [Fact]
    public void Currency_WithFloorsMeasuredInAnotherCurrency_IsThatCurrency()
    {
        // Arrange
        HubProfile profile = ProfileOf(Side(200m, 1.0, "USD"), Side(300m, 4.0, "USD"));

        // Act & Assert
        profile.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_WithFloorsInTwoDifferentCurrencies_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<HubProfile> profile = () => ProfileOf(Side(200m, 1.0, "EUR"), Side(300m, 4.0, "USD"));

        // Assert
        profile.Should()
               .ThrowExactly<ArgumentException>();
    }
}
