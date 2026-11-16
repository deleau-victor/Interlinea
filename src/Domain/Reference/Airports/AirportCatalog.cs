namespace Domain.Reference.Airports;

/// <summary>
/// Catalog providing read-only access to airports.
/// </summary>
public sealed class AirportCatalog
{
    private readonly FrozenDictionary<AirportCode, Airport> _byCode;
    private readonly ILookup<CityCode, Airport> _byCity;

    public AirportCatalog(IEnumerable<Airport> airports)
    {
        var list = airports.ToList();

        var duplicates = list.GroupBy(a => a.Code).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            throw new ArgumentException($"Duplicate airport codes in reference data: {string.Join(", ", duplicates)}.", nameof(airports));

        _byCode = list.ToFrozenDictionary(a => a.Code);
        _byCity = list.ToLookup(a => a.CityCode);
    }

    /// <summary>
    /// Retrieves an airport by it's IATA code.
    /// </summary>
    /// <param name="code">The IATA code to look up.</param>
    /// <returns>The matching <see cref="Airport"/> instance.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the code does not exist in the catalog.</exception>
    public Airport Get(AirportCode code) =>
        _byCode.TryGetValue(code, out Airport? airport)
            ? airport
            : throw new KeyNotFoundException($"Airport missing from the reference data : {code}.");

    /// <summary>
    /// Gets all airports serving a city.
    /// </summary>
    /// <param name="code">The IATA code of the city.</param>
    /// <returns>The airports of the city, or an empty sequence if none.</returns>
    public IEnumerable<Airport> GetByCity(CityCode code) => _byCity[code];

    /// <summary>
    /// Safely attempts to retrieve an airport by its unique IATA code without throwing an exception.
    /// </summary>
    /// <param name="code">The IATA code to look up.</param>
    /// <param name="airport">When this method returns, contains the matching <see cref="Airport"/> if found; otherwise, null.</param>
    /// <returns><see langword="true"/> if the airport was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGet(AirportCode code, [NotNullWhen(true)] out Airport? airport) => _byCode.TryGetValue(code, out airport);

    /// <summary>
    /// Verify if the the code is present in the catlog
    /// </summary>
    /// <param name="code">The IATA code to look up.</param>
    /// <returns><see langword="true"/> if the code exist; otherwise, <see langword="false"/>.</returns>
    public bool Contains(AirportCode code) => _byCode.ContainsKey(code);

    /// <summary>
    /// Determines whether two airports serve the same city
    /// </summary>
    /// <param name="a">The first airport IATA code</param>
    /// <param name="b">The second airport IATA code</param>
    /// <exception cref="KeyNotFoundException">Either code does not exist in the catalog.</exception>
    /// <returns><see langword="true"/> if the two airports are in the same city; otherwise, <see langword="false"/>./returns>
    public bool SameCity(AirportCode a, AirportCode b) => a == b || Get(a).CityCode == Get(b).CityCode;

    /// <summary>
    /// Get the distance between two airports
    /// </summary>
    /// <param name="a">The first airport IATA code</param>
    /// <param name="b">The second airport IATA code</param>
    /// <exception cref="KeyNotFoundException">Either code does not exist in the catalog.</exception>
    /// <returns>The distance between the two airports</returns>
    public double DistanceKm(AirportCode a, AirportCode b) => Get(a).Coords.DistanceKmTo(Get(b).Coords);
}
