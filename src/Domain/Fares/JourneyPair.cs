namespace Domain.Fares;

/// <summary>
/// The shape shared by everything observed as one journey, or as two in opposite directions:
/// a <see cref="Ticket"/> one can buy, and a price merely sighted while probing a route.
/// </summary>
internal static class JourneyPair
{
    /// <summary>
    /// Checks and copies a sequence meant to hold an outbound journey, optionally followed by a return.
    /// </summary>
    /// <param name="journeys">The journeys observed together.</param>
    /// <param name="parameterName">The name of the caller's parameter, for the exception.</param>
    /// <returns>A defensive copy of the journeys.</returns>
    /// <exception cref="ArgumentException">
    /// There is neither one nor two journeys, or the return leaves before the outbound lands.
    /// </exception>
    public static Journey[] Validate(IReadOnlyList<Journey> journeys, string parameterName)
    {
        if (journeys.Count is not (1 or 2))
            throw new ArgumentException("A one-way carries one journey, a round trip two.", parameterName);

        if (journeys.Count == 2 && journeys[1].DepartureAt <= journeys[0].ArrivalAt)
            throw new ArgumentException("The return must leave after the arrival of the outward journey.", parameterName);

        return [.. journeys];
    }
}
