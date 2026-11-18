namespace Domain.Search;

/// <summary>
/// One concrete trip to explore: a chosen departure airport, a departure date, and the return
/// date that the requested stay length implies.
/// </summary>
/// <remarks>
/// This is the unit a worker is actually billed for. A <see cref="SearchRequest"/> describes a
/// space; a candidate is one point of it, and the number of points is what a plan costs.
/// </remarks>
/// <param name="Origin">The departure airport chosen among the accepted ones.</param>
/// <param name="Destination">The arrival airport.</param>
/// <param name="Departure">The date the outbound journey leaves on.</param>
/// <param name="Return">
/// The date the return journey leaves on, or <see langword="null"/> for a one-way. The shape of
/// a one-way is already the right one here; only <see cref="SearchRequest"/> declines to build
/// one for now.
/// </param>
public sealed record TripCandidate(
    AirportCode Origin,
    AirportCode Destination,
    DateOnly Departure,
    DateOnly? Return)
{
    /// <summary>
    /// The arrival airport, which is never one of the departure airports.
    /// </summary>
    /// <exception cref="ArgumentException">The origin and the destination are the same airport.</exception>
    public AirportCode Destination { get; } = Destination != Origin
        ? Destination
        : throw new ArgumentException("A trip cannot start and end at the same airport.", nameof(Destination));

    /// <summary>
    /// The date the return journey leaves on, or <see langword="null"/> for a one-way.
    /// </summary>
    /// <exception cref="ArgumentException">The return leaves before the outbound.</exception>
    public DateOnly? Return { get; } = Return is null || Return >= Departure
        ? Return
        : throw new ArgumentException("The return cannot leave before the departure.", nameof(Return));

    /// <summary>Whether this candidate covers both directions.</summary>
    public bool IsRoundTrip => Return is not null;

    /// <summary>
    /// The stay length at the destination, in nights; zero for a one-way, and zero as well for
    /// a trip that comes back the same day.
    /// </summary>
    public int Nights => Return is { } back ? back.DayNumber - Departure.DayNumber : 0;
}
