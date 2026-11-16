using Domain.Reference.Airports;

namespace Domain.Search.Planning;

/// <summary>
/// The cache key for everything measured per departure point.
/// Hub profiles are kept per city, not per airport, so that a profile probed from one airport of a city serves a search that accepts any airport of that same city.
/// </summary>
internal static class OriginCityKey
{
    /// <summary>
    /// The cities the given airports belong to.
    /// </summary>
    /// <param name="airports">The airport catalog.</param>
    /// <param name="origins">The departure airports.</param>
    /// <returns>The distinct cities of these airports.</returns>
    /// <exception cref="KeyNotFoundException">One of the airports is missing from the catalog.</exception>
    public static IReadOnlySet<CityCode> Cities(AirportCatalog airports, IEnumerable<AirportCode> origins) =>
        origins.Select(origin => airports.Get(origin).CityCode).ToFrozenSet();

    /// <summary>
    /// The key of a set of cities: their codes, deduplicated, ordinally sorted, comma-separated.
    /// </summary>
    /// <param name="cities">The cities to key.</param>
    /// <returns>A stable key, equal for two sets holding the same cities.</returns>
    public static string Of(IEnumerable<CityCode> cities) =>
        string.Join(",", cities.Select(city => city.Value).Distinct().Order(StringComparer.Ordinal));

    /// <summary>
    /// The key of the cities the given airports belong to.
    /// </summary>
    /// <param name="airports">The airport catalog.</param>
    /// <param name="origins">The departure airports.</param>
    /// <returns>A stable key, equal for two airport sets covering the same cities.</returns>
    /// <exception cref="KeyNotFoundException">One of the airports is missing from the catalog.</exception>
    public static string Of(AirportCatalog airports, IEnumerable<AirportCode> origins) =>
        Of(Cities(airports, origins));
}
