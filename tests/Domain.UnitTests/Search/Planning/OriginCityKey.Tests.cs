using Domain.Reference.Airports;
using Domain.Search.Planning;

namespace Domain.UnitTests.Search.Planning;

/// <remarks>
/// This key is computed in one process and compared against a value read back from the
/// database in another, so its stability is a contract of the function itself and not of
/// its current callers. That is why it is tested here, directly, and with ordered lists:
/// given a set, the order of the input would be the hash order and the test could not tell
/// a sorted key from an unsorted one.
/// </remarks>
public sealed class OriginCityKeyTests
{
    private static CityCode[] Cities(params string[] codes) => [.. codes.Select(code => new CityCode(code))];

    private static AirportCode[] Airports(params string[] codes) => [.. codes.Select(code => new AirportCode(code))];

    // —————————————————————— KEY STABILITY TESTS ——————————————————————

    [Fact]
    public void Of_WithTheSameCitiesGivenInAnotherOrder_IsTheSameKey()
    {
        // Arrange & Act
        string madridFirst = OriginCityKey.Of(Cities("MAD", "PAR"));
        string parisFirst = OriginCityKey.Of(Cities("PAR", "MAD"));

        // Assert
        madridFirst.Should().Be("MAD,PAR");
        parisFirst.Should().Be("MAD,PAR");
    }

    [Fact]
    public void Of_WithTheSameCityTwice_CountsItOnce()
    {
        // Arrange & Act & Assert
        OriginCityKey.Of(Cities("PAR", "PAR")).Should().Be("PAR");
    }

    // —————————————————————— CITY LOOKUP TESTS ——————————————————————

    [Fact]
    public void Cities_WithTwoAirportsOfTheSameCity_YieldsThatCityOnce()
    {
        // Arrange & Act
        IReadOnlySet<CityCode> cities = OriginCityKey.Cities(Flights.Airports, Airports("CDG", "ORY"));

        // Assert
        cities.Should().ContainSingle()
              .Which.Should().Be(new CityCode("PAR"));
    }

    [Fact]
    public void Of_WithAirports_IsTheKeyOfTheirCitiesAndNotOfTheirCodes()
    {
        // Arrange & Act — CDG and ORY are both PAR: the key names cities, never airports.
        string key = OriginCityKey.Of(Flights.Airports, Airports("ORY", "CDG", "MAD"));

        // Assert
        key.Should().Be("MAD,PAR");
    }

    [Fact]
    public void Cities_WithAnAirportMissingFromTheCatalog_ThrowsKeyNotFoundException()
    {
        // Arrange & Act
        Func<IReadOnlySet<CityCode>> cities = () => OriginCityKey.Cities(Flights.Airports, Airports("BOG"));

        // Assert
        cities.Should()
              .ThrowExactly<KeyNotFoundException>();
    }
}
