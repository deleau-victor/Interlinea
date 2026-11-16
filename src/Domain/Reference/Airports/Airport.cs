namespace Domain.Reference.Airports;

public sealed class Airport
{
    /// <summary>
    /// IATA airport code
    /// </summary>
    public AirportCode Code { get; }

    /// <summary>
    /// Airport name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// IATA city code
    /// </summary>
    public CityCode CityCode { get; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code
    /// </summary>
    public CountryCode Country { get; }

    /// <summary>
    /// Timezone of the Airport
    /// </summary>
    public TimeZoneInfo TimeZone { get; }

    /// <summary>
    /// Location of the airport
    /// </summary>
    public GeoPoint Coords { get; }

    public Airport(AirportCode code, string name, CityCode cityCode, CountryCode country, string timeZoneId, GeoPoint coords)
    {
        (Code, Name, CityCode, Country, Coords) = (code, name, cityCode, country, coords);
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    /// <summary>
    /// Convert a local date and a local time into an usable DateTimeOffset
    /// </summary>
    /// <param name="date">The local date</param>
    /// <param name="time">The local Time</param>
    /// <returns>A DateTimeOffset</returns>
    public DateTimeOffset At(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, TimeZone.GetUtcOffset(local));
    }

    /// <inheritdoc/>
    public override string ToString() => Code.Value;
}
