namespace Domain.Fares;

public sealed class Journey
{
    public IReadOnlyList<Segment> Segments { get; }
    public Journey(params Segment[] segments) : this((IEnumerable<Segment>)segments) { }

    public Journey(IEnumerable<Segment> segments)
    {
        Segments = [.. segments];
        if (Segments.Count == 0)
            throw new ArgumentException("A journey must contain at least one segment.", nameof(segments));

        for (int i = 1; i < Segments.Count; i++)
        {
            if (Segments[i].DepartureAt <= Segments[i - 1].ArrivalAt)
                throw new ArgumentException($"The segments are not in chronological order : {Segments[i - 1].Designator} arrives at {Segments[i - 1].ArrivalAt} and {Segments[i].Designator} departs at {Segments[i].DepartureAt}.", nameof(segments));
        }
    }

    /// <summary>
    /// The origin airport of the journey.
    /// </summary>
    public AirportCode Origin => Segments[0].From;

    /// <summary>
    /// The destination airport of the journey.
    /// </summary>
    public AirportCode Destination => Segments[^1].To;

    /// <summary>
    /// The departure time of the journey.
    /// </summary>
    public DateTimeOffset DepartureAt => Segments[0].DepartureAt;

    /// <summary>
    /// The arrival time of the journey.
    /// </summary>
    public DateTimeOffset ArrivalAt => Segments[^1].ArrivalAt;

    /// <summary>
    /// The computed duration of the journey.
    /// </summary>
    public TimeSpan Duration => ArrivalAt - DepartureAt;

    /// <summary>
    /// The departure date of the journey.
    /// </summary>
    public DateOnly DepartureDate => DateOnly.FromDateTime(DepartureAt.DateTime);

    /// <summary>
    /// Gets the layovers between segments in the journey.
    /// </summary>
    public IEnumerable<TimeSpan> Layovers => Segments.Zip(Segments.Skip(1), (prev, next) => next.DepartureAt - prev.ArrivalAt);

    /// <summary>
    /// The key of the journey.
    /// The physical key is a unique identifier for the journey, based on the segments' physical keys.
    /// </summary>
    public string Key => string.Join(">", Segments.Select(s => s.PhysicalKey));
}
