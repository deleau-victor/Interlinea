using Domain.Search.Planning;

namespace Domain.UnitTests.Search.Planning;

public sealed class PriceSightingTests
{
    private static Journey OutboundLeg => Flights.Journey("CDG", "MAD", Flights.Outbound, duration: 2);
    private static Journey InboundLeg => Flights.Journey("MAD", "CDG", Flights.Inbound, duration: 2);

    private static PriceSighting Sighting(params Journey[] journeys) =>
        new(journeys, new Money(110m, "EUR"));

    // —————————————————————— SHAPE TESTS ——————————————————————

    [Fact]
    public void ReturnJourney_OfASightingOnOneJourney_IsNull()
    {
        // Arrange & Act
        PriceSighting sighting = Sighting(OutboundLeg);

        // Assert
        sighting.ReturnJourney.Should().BeNull();
        sighting.FirstJourney.Origin.Should().Be(new AirportCode("CDG"));
    }

    [Fact]
    public void FirstAndReturnJourney_OfASightingOnTwoJourneys_AreReadInThatOrder()
    {
        // Arrange & Act
        PriceSighting sighting = Sighting(OutboundLeg, InboundLeg);

        // Assert
        sighting.FirstJourney.Origin.Should().Be(new AirportCode("CDG"));
        sighting.ReturnJourney.Should().NotBeNull();
        sighting.ReturnJourney!.Origin.Should().Be(new AirportCode("MAD"));
    }

    // —————————————————————— CONSTRUCTOR TESTS ——————————————————————

    [Fact]
    public void Constructor_WithoutAnyJourney_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<PriceSighting> sighting = () => Sighting();

        // Assert
        sighting.Should()
                .ThrowExactly<ArgumentException>().Which.ParamName
                .Should()
                .Be(nameof(PriceSighting.Journeys));
    }

    [Fact]
    public void Constructor_WithThreeJourneys_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<PriceSighting> sighting = () => Sighting(OutboundLeg, InboundLeg, OutboundLeg);

        // Assert
        sighting.Should()
                .ThrowExactly<ArgumentException>().Which.ParamName
                .Should()
                .Be(nameof(PriceSighting.Journeys));
    }

    [Fact]
    public void Constructor_WithAReturnLeavingBeforeTheOutboundLands_ThrowsArgumentException()
    {
        // Arrange
        Journey outbound = Flights.Journey("CDG", "LIM", Flights.Outbound);
        Journey tooEarly = Flights.Journey("LIM", "CDG", Flights.Outbound.AddHours(5));

        // Act
        Func<PriceSighting> sighting = () => Sighting(outbound, tooEarly);

        // Assert
        sighting.Should()
                .ThrowExactly<ArgumentException>().Which.ParamName
                .Should()
                .Be(nameof(PriceSighting.Journeys));
    }

    [Fact]
    public void Constructor_WithAReturnLeavingExactlyWhenTheOutboundLands_ThrowsArgumentException()
    {
        // Arrange
        Journey outbound = Flights.Journey("CDG", "LIM", Flights.Outbound);
        Journey onTheDot = Flights.Journey("LIM", "CDG", outbound.ArrivalAt);

        // Act
        Func<PriceSighting> sighting = () => Sighting(outbound, onTheDot);

        // Assert
        sighting.Should()
                .ThrowExactly<ArgumentException>();
    }
}
