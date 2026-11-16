namespace Domain.Fares;

public sealed record Segment(
    FlightDesignator Designator,
    CarrierCode OperatingCarrier,
    IReadOnlyList<FlightDesignator> Codeshares,
    AirportCode From,
    AirportCode To,
    DateTimeOffset DepartureAt,
    DateTimeOffset ArrivalAt,
    Cabin Cabin)
{
    #region  ——————————— Invariants ———————————
    public IReadOnlyList<FlightDesignator> Codeshares { get; } = [.. Codeshares];

    public AirportCode To { get; } = To != From
        ? To
        : throw new ArgumentException("A segment cannot arrive at it's departure airport.", nameof(To));

    public DateTimeOffset ArrivalAt { get; } = ArrivalAt > DepartureAt
        ? ArrivalAt
        : throw new ArgumentException("The arrival time must be after the departure time.", nameof(ArrivalAt));

    #endregion

    #region ——————————— Product attribute's ———————————

    /// <summary>
    /// The aircraft operated on this segment, if known.
    /// </summary>
    public string? Aircraft { get; init; }

    /// <summary>
    /// The pitch (legroom) in cm of the aircraft operated on this segment, if known.
    /// </summary>
    public string? Pitch { get; init; }

    /// <summary>
    /// The CO2 emissions in grams for this segment, if known.
    /// </summary>
    public int? Co2Grams { get; init; }

    /// <summary>
    /// The announced duration of this segment, if known.
    /// </summary>
    public TimeSpan? AnnouncedDuration { get; init; }

    #endregion

    #region —————————— Computed properties ———————————

    /// <summary>
    /// The computed duration
    /// Computed from the departure and arrival times.
    /// </summary>
    public TimeSpan Duration => ArrivalAt - DepartureAt;

    /// <summary>
    /// Indicates whether this segment is a codeshare flight.
    /// </summary>
    public bool IsCodeshare => OperatingCarrier != Designator.Carrier;

    /// <summary>
    /// Gets all flight designators for this segment, including the operating carrier and any codeshares.
    /// </summary>
    public IEnumerable<FlightDesignator> AllDesignators => Codeshares.Prepend(Designator);

    /// <summary>
    /// False if the announced duration is known and differs from the computed duration by more than 5 minutes, true otherwise.
    /// If false, indicates that the timezone of an airport has not been greatly resolved.
    /// </summary>
    public bool HasConsistentTimeZones =>
        AnnouncedDuration is not { } announced || (Duration - announced).Duration() <= TimeSpan.FromMinutes(5);

    /// <summary>
    /// Identify a segment independently of the flight number under it is sold.
    /// An OTA can sell a flight from AF under his KL number.
    /// Can be also used for caching.
    /// </summary>
    public string PhysicalKey => $"{OperatingCarrier}:{From}-{To}@{DepartureAt.UtcDateTime:yyyyMMddHHmm}";

    #endregion
}
