namespace Domain.Common;

/// <summary>
/// Geographic position in decimal degrees
/// </summary>
/// <param name="Latitude">Between -90° (South pole) and 90° (North pole)</param>
/// <param name="Longitude">Between -180° and 180° around Greenwich.</param>
public readonly record struct GeoPoint(double Latitude, double Longitude)
{
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Between -90° (South pole) and 90° (North pole)
    /// </summary>
    public double Latitude { get; } = Latitude is >= -90 and <= 90
        ? Latitude
        : throw new ArgumentOutOfRangeException(nameof(Latitude));

    /// <summary>
    /// Between -180° and 180° around Greenwich.
    /// </summary>
    public double Longitude { get; } = Longitude is >= -180 and <= 180
        ? Longitude
        : throw new ArgumentOutOfRangeException(nameof(Longitude));

    /// <summary>
    /// Compute the distance on the globe with an other point using Haversine formula.
    /// </summary>
    /// <param name="other">Th other point</param>
    /// <returns>The distance between the two points</returns>
    public double DistanceKmTo(GeoPoint other)
    {
        double deltaLatitudeRad = Radians(other.Latitude - Latitude);
        double deltaLongitudeRad = Radians(other.Longitude - Longitude);

        double latitudeComponent = Math.Pow(Math.Sin(deltaLatitudeRad / 2), 2);
        double longitudeComponent = Math.Cos(Radians(Latitude)) * Math.Cos(Radians(other.Latitude)) * Math.Pow(Math.Sin(deltaLongitudeRad / 2), 2);
        double halfChordSquare = latitudeComponent + longitudeComponent;

        double angularDistanceRad = 2 * Math.Asin(Math.Sqrt(halfChordSquare));
        double distanceKm = angularDistanceRad * EarthRadiusKm;

        return distanceKm;
    }

    private static double Radians(double degrees) => degrees * Math.PI / 180;
}
