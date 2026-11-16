using Domain.Fares;
using Domain.Reference.Airports;

namespace Domain.Search.Planning.Partitions;

/// <summary>
/// Describes the legs of a partition: which routes to search, on which dates,
/// and whether a journey or a ticket matches a given side and direction.
/// </summary>
internal sealed class PartitionLegs
{
    /// <summary>
    /// Maximum number of days between the departure of the first flight and the departure of the second flight of a connection.
    /// </summary>
    private const int MaxConnectionDayOffset = 1;
    private readonly AirportCatalog _airports;
    private readonly SearchRequest _request;
    private readonly IReadOnlyList<AirportCode> _origins;
    private readonly CityCode _hubCity;

    /// <exception cref="KeyNotFoundException">The hub or one of the origins is missing from the airport catalog.</exception>
    public PartitionLegs(AirportCatalog airports, SearchRequest request, Partition partition)
    {
        _airports = airports;
        _request = request;
        Partition = partition;

        _origins = [.. request.Origins.OrderBy(o => o.Value, StringComparer.Ordinal)];
        _hubCity = airports.Get(partition.Hub).CityCode;
        OriginCityCodes = request.Origins
            .Select(o => airports.Get(o).CityCode)
            .ToFrozenSet();
        OriginCities = string.Join(",", OriginCityCodes
            .Select(cityCode => cityCode.Value)
            .Distinct()
            .Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// Both travel directions, outward first.
    /// </summary>
    public static IReadOnlyList<TravelDirection> Directions { get; } =
        Array.AsReadOnly([TravelDirection.Outward, TravelDirection.Return]);

    /// <summary>
    /// The partition these legs belong to.
    /// </summary>
    public Partition Partition { get; }

    /// <summary>
    /// The distinct city codes of the requested origins, sorted ordinally and comma-separated.
    /// Used to make a cache key.
    /// </summary>
    public string OriginCities { get; }

    public IReadOnlySet<CityCode> OriginCityCodes { get; }

    /// <summary>
    /// Gets the routes to search for a side of the trip.
    /// </summary>
    /// <param name="side">The side of the trip.</param>
    /// <param name="direction">The direction of the trip.</param>
    /// <param name="hubAirport">
    /// A specific hub airport, used when searching for the second flight once the hub has been chosen.
    /// Defaults to the partition hub.
    /// </param>
    /// <returns>The routes for this side and direction.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The side/direction combination is not supported.</exception>
    public IReadOnlyList<Route> Routes(TripSide side, TravelDirection direction, AirportCode? hubAirport = null)
    {
        AirportCode hub = hubAirport ?? Partition.Hub;
        return (side, direction) switch
        {
            (TripSide.Origin, TravelDirection.Outward) => [.. _origins.Select(o => new Route(o, hub))],
            (TripSide.Origin, TravelDirection.Return) => [.. _origins.Select(o => new Route(hub, o))],
            (TripSide.Destination, TravelDirection.Outward) => [new Route(hub, _request.Destination)],
            (TripSide.Destination, TravelDirection.Return) => [new Route(_request.Destination, hub)],
            _ => throw Unsupported(side, direction),
        };
    }

    /// <summary>
    /// Gets the departure dates window for a side of the trip.
    /// </summary>
    /// <param name="side">The side of the trip.</param>
    /// <param name="direction">The direction of the trip.</param>
    /// <returns>The dates on which a flight on this side can depart.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The side/direction combination is not supported.</exception>
    public DateRange Window(TripSide side, TravelDirection direction) => (side, direction) switch
    {
        (TripSide.Origin, TravelDirection.Outward) => _request.OutboundDates,
        (TripSide.Destination, TravelDirection.Outward) => _request.OutboundDates.Extend(0, MaxConnectionDayOffset),
        (TripSide.Destination, TravelDirection.Return) => _request.ReturnDates,
        (TripSide.Origin, TravelDirection.Return) => _request.ReturnDates.Extend(0, MaxConnectionDayOffset),
        _ => throw Unsupported(side, direction),
    };

    /// <summary>
    /// Determines whether a journey connects the expected endpoints, within the expected dates, for a side of the trip.
    /// </summary>
    /// <param name="journey">The journey to check.</param>
    /// <param name="side">The side of the trip.</param>
    /// <param name="direction">The direction of the trip.</param>
    /// <returns><see langword="true"/> if the journey fits; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The side/direction combination is not supported.</exception>
    public bool Fits(Journey journey, TripSide side, TravelDirection direction) =>
        Window(side, direction).Contains(journey.DepartureDate)
        && (side, direction) switch
        {
            (TripSide.Origin, TravelDirection.Outward) => IsOrigin(journey.Origin) && AtHub(journey.Destination),
            (TripSide.Origin, TravelDirection.Return) => AtHub(journey.Origin) && IsOrigin(journey.Destination),
            (TripSide.Destination, TravelDirection.Outward) => AtHub(journey.Origin) && IsDestination(journey.Destination),
            (TripSide.Destination, TravelDirection.Return) => IsDestination(journey.Origin) && AtHub(journey.Destination),
            _ => throw Unsupported(side, direction),
        };

    /// <summary>
    /// Determines whether the ticket is a round trip covering a side of the trip in both directions.
    /// </summary>
    /// <param name="ticket">The ticket to check.</param>
    /// <param name="side">The side of the trip.</param>
    /// <returns><see langword="true"/> if the ticket is a matching round trip; otherwise, <see langword="false"/>.</returns>
    public bool IsRoundTripOn(Ticket ticket, TripSide side) =>
        ticket.ReturnJourney is { } back
        && Fits(ticket.FirstJourney, side, TravelDirection.Outward)
        && Fits(back, side, TravelDirection.Return);

    /// <summary>
    /// Determines whether the ticket is a one-way covering a side of the trip in a given direction.
    /// </summary>
    /// <param name="ticket">The ticket to check.</param>
    /// <param name="side">The side of the trip.</param>
    /// <param name="direction">The direction of the trip.</param>
    /// <returns><see langword="true"/> if the ticket is a matching one-way; otherwise, <see langword="false"/>.</returns>
    public bool IsOneWayOn(Ticket ticket, TripSide side, TravelDirection direction) =>
        ticket.ReturnJourney is null
        && Fits(ticket.FirstJourney, side, direction);

    // The hub matches by city (e.g. ORY for a CDG hub), whereas origins and destination
    // must match the exact airport codes requested by the user.
    // Unknown airports never match instead of throwing.
    private bool AtHub(AirportCode airport) =>
        airport == Partition.Hub
        || (_airports.TryGet(airport, out Airport? a) && a.CityCode == _hubCity);

    private bool IsOrigin(AirportCode airport) => _request.Origins.Contains(airport);

    private bool IsDestination(AirportCode airport) => airport == _request.Destination;

    private static ArgumentOutOfRangeException Unsupported(TripSide side, TravelDirection direction) =>
        new(nameof(direction), $"Unsupported side/direction combination: {side}/{direction}.");
}
