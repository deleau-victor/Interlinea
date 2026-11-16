using Domain.Fares;

namespace Domain.Search.Planning;

public sealed record PriceQuery(
    Route Route,
    TripType TripType,
    DateRange Departure,
    DateRange? Return,
    BaggageOption Baggage)
{
    public TimeOnly? DepartAfter { get; init; }
    public TimeOnly? ArriveBefore { get; init; }
    public TimeOnly? ReturnDepartAfter { get; init; }
    public TimeOnly? ReturnArriveBefore { get; init; }
}
