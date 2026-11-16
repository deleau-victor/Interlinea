using Domain.Search.Planning.Partitions;

namespace Domain.Search.Planning.Hubs;

/// <summary>
/// The knowing from an observed hub for an O&D couple.
/// Must be conserved 30 days.
/// </summary>
/// <param name="Hub">The observed hub</param>
/// <param name="Origins">The origin city(ies)</param>
/// <param name="Destination">The destination</param>
/// <param name="OriginSide">The measures from the origin side</param>
/// <param name="DestinationSide">The measures from the destination side</param>
/// <param name="ProbedAt">The date of the observation</param>
public sealed record HubProfile
{
    private readonly LegProfile _originSide;
    private readonly LegProfile _destinationSide;

    public AirportCode Hub { get; }
    public AirportCode Destination { get; }
    public DateTimeOffset ProbedAt { get; }

    /// <summary>
    /// The distinct city codes of the requested origins, sorted ordinally and comma-separated.
    /// Used to make a cache key.
    /// </summary>
    public string OriginCities { get; }

    public HubProfile(
        AirportCode hub,
        IReadOnlySet<CityCode> origins,
        AirportCode destination,
        LegProfile originSide,
        LegProfile destinationSide,
        DateTimeOffset probedAt)
    {
        Hub = hub;
        Destination = destination;
        _originSide = originSide;
        _destinationSide = destinationSide;
        ProbedAt = probedAt;

        OriginCities = string.Join(",", origins
            .Select(o => o.Value)
            .Distinct()
            .Order(StringComparer.Ordinal));
    }

    public IReadOnlyList<PartitionEstimate> Candidates()
    {
        LegPricing[] modes = [LegPricing.OneWays, LegPricing.RoundTrip];
        List<PartitionEstimate>? candidates =
        [.. (
                from origin in modes
                from destination in modes

                let originFloor = _originSide.FloorFor(origin)
                let destinationFloor = _destinationSide.FloorFor(destination)

                where originFloor is not null && destinationFloor is not null

                let partition = new Partition(Hub, origin, destination)
                let partitionPlan = new PartitionPlan(partition, AnchorFor(originFloor.Value, destinationFloor.Value))

                select new PartitionEstimate(
                    partitionPlan,
                    originFloor.Value + destinationFloor.Value
                )
            )
            .OrderBy(e => e.Floor)
        ];

        return candidates;
    }

    private TripSide AnchorFor(Money originFloor, Money destinationFloor)
    {
        if (_originSide.FlightsPerDay != _destinationSide.FlightsPerDay)
        {
            return _originSide.FlightsPerDay < _destinationSide.FlightsPerDay
                ? TripSide.Origin
                : TripSide.Destination;
        }

        return originFloor > destinationFloor
            ? TripSide.Origin
            : TripSide.Destination;
    }
}
