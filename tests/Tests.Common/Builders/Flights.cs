
using Domain.Fares;
using Domain.Reference.Airports;
using Domain.TravelRules;

namespace Tests.Common.Builders;

/// <summary>
/// Minimal fixtures: airports, flights, journeys and tickets, so that each test only states what it actually cares about.
/// </summary>
public static class Flights
{
    public static readonly DateTimeOffset Outbound = new(2026, 12, 1, 10, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset Inbound = new(2026, 12, 20, 10, 0, 0, TimeSpan.Zero);

    public static AirportCatalog Airports { get; } = new(
    [
        Airport("CDG", "PAR", "FR", "Europe/Paris", 49.0097, 2.5479),
        Airport("ORY", "PAR", "FR", "Europe/Paris", 48.7233, 2.3794),
        Airport("MAD", "MAD", "ES", "Europe/Madrid", 40.4719, -3.5626),
        Airport("YUL", "YMQ", "CA", "America/Toronto", 45.4706, -73.7408),
        Airport("LIM", "LIM", "PE", "America/Lima", -12.0219, -77.1143),
    ]);

    public static ConnectionPolicy Policy { get; } = new()
    {
        SelfTransferCabinOnly = TimeSpan.FromHours(2),
        SelfTransferCheckedBag = TimeSpan.FromHours(4),
        MaxLayover = TimeSpan.FromHours(24),
        OvernightThreshold = TimeSpan.FromHours(10),
        EstimatedHotelNight = new Money(80m, "EUR"),
    };

    private static Airport Airport(string airportCode, string cityCode, string countryCode, string timezone, double lat, double lon) =>
        new(new AirportCode(airportCode), airportCode, new CityCode(cityCode), new CountryCode(countryCode), timezone, new GeoPoint(lat, lon));

    public static Segment Flight(string from, string to, DateTimeOffset departureAt, double duration = 12) =>
        new(FlightDesignator.Parse("AF123"), new CarrierCode("AF"), [],
            new AirportCode(from), new AirportCode(to), departureAt, departureAt + TimeSpan.FromHours(duration), Cabin.Economy);

    public static Journey Journey(string from, string to, DateTimeOffset departureAt, double duration = 12) =>
        new(Flight(from, to, departureAt, duration));

    public static Ticket Ticket(decimal price, string currency, params Journey[] journeys) =>
        new(Guid.NewGuid(), journeys, new Money(price, currency),
            new Seller("Air France", SellerKind.Carrier), CheckedBagIncluded: false, DateTimeOffset.UtcNow);
}
