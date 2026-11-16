namespace Domain.Common;

/// <summary>
/// Inclusive range of dates, defined by a start date (From) and an end date (To).
/// </summary>
public sealed record DateRange(DateOnly From, DateOnly To)
{
    public DateOnly To { get; } = To >= From
        ? To
        : throw new ArgumentException("The end date (To) must be greater than or equal to the start date (From).", nameof(To));

    /// <summary>
    /// Represent an unique Day
    /// </summary>
    /// <param name="date">The date one wishes to represent</param>
    /// <returns>A DateRange with the same From and To date</returns>
    public static DateRange Unique(DateOnly date) => new(date, date);

    /// <summary>
    /// Determines whether the specified date falls within the date range (inclusive).
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if the date is within the range; otherwise, false.</returns>
    public bool Contains(DateOnly date) => date >= From && date <= To;

    /// <summary>
    /// Extends the date range by a specified number of days before and after the current range.
    /// </summary>
    /// <param name="daysBefore">The number of days to extend before the current start date (From).</param>
    /// <param name="daysAfter">The number of days to extend after the current end date (To).</param>
    /// <returns>A new DateRange instance with the extended range.</returns>
    public DateRange Extend(int daysBefore, int daysAfter) => new(From.AddDays(-daysBefore), To.AddDays(daysAfter));

    /// <summary>
    /// The numbers of days in the range
    /// </summary>
    public int DayCount => To.DayNumber - From.DayNumber + 1;
}
