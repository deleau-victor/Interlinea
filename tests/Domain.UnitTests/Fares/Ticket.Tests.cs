namespace Domain.UnitTests.Fares;

public sealed class TicketTests
{
    // —————————————————————— CONSTRUCTOR TESTS ——————————————————————

    [Fact]
    public void Constructor_WithoutAnyJourney_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<Ticket> ticket = () => Flights.Ticket(110m, "EUR");

        // Assert
        ticket.Should()
              .ThrowExactly<ArgumentException>().Which.ParamName
              .Should()
              .Be(nameof(Ticket.Journeys));
    }

    [Fact]
    public void Constructor_WithThreeJourneys_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<Ticket> ticket = () => Flights.Ticket(
            110m,
            "EUR",
            Flights.Journey("CDG", "MAD", Flights.Outbound, duration: 2),
            Flights.Journey("MAD", "CDG", Flights.Inbound, duration: 2),
            Flights.Journey("CDG", "MAD", Flights.Inbound.AddDays(1), duration: 2));

        // Assert
        ticket.Should()
              .ThrowExactly<ArgumentException>().Which.ParamName
              .Should()
              .Be(nameof(Ticket.Journeys));
    }

    [Fact]
    public void Constructor_WithAReturnLeavingBeforeTheOutboundLands_ThrowsArgumentException()
    {
        // Arrange
        Journey outbound = Flights.Journey("CDG", "LIM", Flights.Outbound);
        Journey tooEarly = Flights.Journey("LIM", "CDG", Flights.Outbound.AddHours(5));

        // Act
        Func<Ticket> ticket = () => Flights.Ticket(110m, "EUR", outbound, tooEarly);

        // Assert
        ticket.Should()
              .ThrowExactly<ArgumentException>().Which.ParamName
              .Should()
              .Be(nameof(Ticket.Journeys));
    }
}
