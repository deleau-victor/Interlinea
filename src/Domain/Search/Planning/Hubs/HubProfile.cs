using Domain.Search.Planning.Partitions;

namespace Domain.Search.Planning.Hubs;

/// <summary>
/// The knowing from an observed hub for an O&amp;D couple.
/// Must be conserved 30 days.
/// </summary>
/// <remarks>
/// A profile outlives the search that produced it, and the next search may ask for another
/// currency. Its floors are therefore held in a single pivot currency, and
/// <see cref="Candidates(ExchangeRates, string)"/> is the only way to read them: it converts,
/// so no caller can compare a cached floor against a price in a currency it was not measured in.
/// </remarks>
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

    /// <summary>
    /// The pivot currency the measured floors are held in,
    /// or <see langword="null"/> when nothing was measured on either side.
    /// </summary>
    public string? Currency { get; }

    /// <param name="hub">The observed hub.</param>
    /// <param name="origins">The origin city(ies).</param>
    /// <param name="destination">The destination.</param>
    /// <param name="originSide">The measures from the origin side.</param>
    /// <param name="destinationSide">The measures from the destination side.</param>
    /// <param name="probedAt">The date of the observation.</param>
    /// <exception cref="ArgumentException">The two sides were measured in different currencies.</exception>
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

        OriginCities = OriginCityKey.Of(origins);

        string[] currencies = [.. Floors(originSide, destinationSide).Select(floor => floor.Currency).Distinct()];
        Currency = currencies.Length switch
        {
            0 => null,
            1 => currencies[0],
            _ => throw new ArgumentException(
                $"A hub profile holds its floors in one pivot currency, found: {string.Join(", ", currencies)}.", nameof(originSide)),
        };
    }

    private static IEnumerable<Money> Floors(LegProfile origin, LegProfile destination) =>
        new[] { origin.OneWaysFloor, origin.RoundTripFloor, destination.OneWaysFloor, destination.RoundTripFloor }
            .Where(floor => floor is not null)
            .Select(floor => floor!.Value);

    /// <summary>
    /// The partitions this hub allows, cheapest estimated floor first.
    /// </summary>
    /// <param name="rates">The exchange rates used to leave the pivot currency.</param>
    /// <param name="currency">The currency the floors are wanted in, normally the search currency.</param>
    /// <returns>
    /// One estimate per pair of pricing modes actually measured on both sides,
    /// its floor expressed in <paramref name="currency"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">No rate converts the pivot currency into <paramref name="currency"/>.</exception>
    public IReadOnlyList<PartitionEstimate> Candidates(ExchangeRates rates, string currency)
    {
        LegPricing[] modes = [LegPricing.OneWays, LegPricing.RoundTrip];
        List<PartitionEstimate> candidates =
        [.. (
                from origin in modes
                from destination in modes

                let originFloor = _originSide.FloorFor(origin)
                let destinationFloor = _destinationSide.FloorFor(destination)

                where originFloor is not null && destinationFloor is not null

                let partition = new Partition(Hub, origin, destination)
                let partitionPlan = new PartitionPlan(partition, AnchorFor(originFloor.Value, destinationFloor.Value))

                // Summed in the pivot currency, converted once: two conversions would round twice.
                select new PartitionEstimate(
                    partitionPlan,
                    rates.Convert(originFloor.Value + destinationFloor.Value, currency)
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
