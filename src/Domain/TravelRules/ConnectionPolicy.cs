namespace Domain.TravelRules;

/// <summary>
/// The rules of a self transfer
/// </summary>
public sealed record ConnectionPolicy
{
    /// <summary>Minimum time between two flights when traveling with carry-on baggage only</summary>
    public required TimeSpan SelfTransferCabinOnly { get; init; }

    /// <summary>Minimum time between two flights when traveling with checked baggages</summary>
    public required TimeSpan SelfTransferCheckedBag { get; init; }

    /// <summary>Maximum allowed time between two tickets</summary>
    public required TimeSpan MaxLayover { get; init; }

    /// <summary>Minimum number of hours after which a night's stay is counted</summary>
    public required TimeSpan OvernightThreshold { get; init; }

    /// <summary>Estimated cost of a night at a hotel</summary>
    public required Money EstimatedHotelNight { get; init; }

    /// <summary>A margin specific to an airport and a type of baggage, it replaces the general margin</summary>
    public IReadOnlyDictionary<(AirportCode Airport, BaggageOption Baggage), TimeSpan> AirportOverrides { get; init; }
        = new Dictionary<(AirportCode, BaggageOption), TimeSpan>();

    /// <summary>Known ground routes between airports in the same city</summary>
    public IReadOnlyList<AirportTransfer> Transfers { get; init; } = [];

    /// <summary>Minimum time between the arrival of one flight and the departure of the next</summary>
    /// <param name="arrival">Airport where the first ticket arrives</param>
    /// <param name="departure">Airport from which the second flight departs</param>
    /// <param name="baggage">The baggages options of the traveler</param>
    /// <returns>
    /// The margin, plus the ground transportation cost if the airports are different.
    /// <see langword="null"/> If the airports are different and there is no known route between them, a connection is not possible
    /// </returns>
    public TimeSpan? MinimumSelfTransfer(AirportCode arrival, AirportCode departure, BaggageOption baggage)
    {
        TimeSpan minimum = AirportOverrides.TryGetValue((departure, baggage), out TimeSpan specific)
            ? specific
            : baggage == BaggageOption.CheckedBaggages ? SelfTransferCheckedBag : SelfTransferCabinOnly;

        if (arrival == departure)
            return minimum;
        return TransferBetween(arrival, departure) is { } transfer ? minimum + transfer.Duration : null;
    }

    /// <summary>
    /// Is a self-transfer possible between this arrival and this departure: with the minimum layover met and a wait time no longer than <see cref="MaxLayover"/>?
    /// </summary>
    public bool AllowsSelfTransfer(
        AirportCode arrivalAirport, DateTimeOffset arrival,
        AirportCode departureAirport, DateTimeOffset departure,
        BaggageOption baggage)
    {
        TimeSpan? minimum = MinimumSelfTransfer(arrivalAirport, departureAirport, baggage);
        TimeSpan layover = departure - arrival;
        return minimum is not null && layover >= minimum && layover <= MaxLayover;
    }

    /// <summary>The known land route between two airports, if any</summary>
    public AirportTransfer? TransferBetween(AirportCode from, AirportCode to) =>
        Transfers.FirstOrDefault(t => t.From == from && t.To == to);
}
