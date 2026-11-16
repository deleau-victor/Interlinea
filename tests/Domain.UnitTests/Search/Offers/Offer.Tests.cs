using Domain.Search.Offers;
using Domain.Search.Offers.AncillaryCosts;
using Domain.Search.Ranking;

namespace Domain.UnitTests.Search.Offers;

public sealed class OfferTests
{
    /// <summary>
    /// Two round-trip tickets self-connecting at Madrid, 600 and 300.
    /// </summary>
    private static Offer ViaMadrid()
    {
        Journey toMadrid = Flights.Journey("CDG", "MAD", Flights.Outbound, duration: 2);
        Journey toLima = Flights.Journey("MAD", "LIM", Flights.Outbound.AddHours(5));
        Journey fromLima = Flights.Journey("LIM", "MAD", Flights.Inbound);
        Journey fromMadrid = Flights.Journey("MAD", "CDG", Flights.Inbound.AddHours(22), duration: 2);

        Ticket feeder = Flights.Ticket(600m, "EUR", toMadrid, fromMadrid);
        Ticket longHaul = Flights.Ticket(300m, "EUR", toLima, fromLima);

        return new Offer(
            [new OfferedTicket(feeder, []), new OfferedTicket(longHaul, [])],
            [toMadrid, toLima],
            [fromLima, fromMadrid],
            via: null);
    }

    private static AncillaryCost Ancillary(AncillaryKind kind, string label, decimal amount, string currency = "EUR") =>
        new(kind, label, new Money(amount, currency), new Money(amount, currency));

    // —————————————————————— COMPUTED PROPERTIES TESTS ——————————————————————

    [Fact]
    public void TotalCost_WithoutAnyAncillaryCost_SumsEveryTicket()
    {
        // Arrange & Act
        Offer offer = ViaMadrid();

        // Assert
        offer.AncillaryCosts.Should().BeEmpty();
        offer.AncillaryTotal.Should().Be(new Money(0m, "EUR"));
        offer.TicketsPrice.Should().Be(new Money(900m, "EUR"));
        offer.TotalCost.Should().Be(new Money(900m, "EUR"));
    }

    [Fact]
    public void TotalCost_WithAncillaryCosts_SumsEveryTicketAndEveryAncillary()
    {
        // Arrange
        Offer offer = ViaMadrid() with
        {
            AncillaryCosts =
            [
                Ancillary(AncillaryKind.HotelNight, "Night at MAD", 80m),
                Ancillary(AncillaryKind.GroundTransfer, "CDG to ORY", 25m),
            ],
        };

        // Act & Assert
        offer.AncillaryTotal.Should().Be(new Money(105m, "EUR"));
        offer.TotalCost.Should().Be(new Money(1005m, "EUR"));
    }

    [Fact]
    public void TotalCost_WithAnAncillaryCostInAnotherCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        Offer offer = ViaMadrid() with
        {
            AncillaryCosts = [Ancillary(AncillaryKind.HotelNight, "Night at MAD", 80m, "USD")],
        };

        // Act
        Func<Money> total = () => offer.TotalCost;

        // Assert
        total.Should()
             .ThrowExactly<InvalidOperationException>();
    }
}
