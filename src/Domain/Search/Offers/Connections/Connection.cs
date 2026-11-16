using Domain.Fares;

namespace Domain.Search.Offers.Connections;

/// <summary>
/// A layover: the arrival of one flight and the departure of the next.
/// </summary>
/// <param name="Arriving">The flight who arrives</param>
/// <param name="Departing">The departing flight</param>
/// <param name="Kind">Protected or Self-Transfer connection</param>
public sealed record Connection(Segment Arriving, Segment Departing, ConnectionKind Kind)
{
    /// <summary>The airport code where the layover takes place</summary>
    public AirportCode At => Arriving.To;

    /// <summary>The waiting time between the 2 flights</summary>
    public TimeSpan Layover => Departing.DepartureAt - Arriving.ArrivalAt;

    /// <summary>Is aground transfer necessary to makes an airport change ?</summary>
    public bool ChangesAirport => Arriving.To != Departing.From;

    /// <summary>
    /// Does the traveler leave the international zone?
    /// </summary>
    public bool CrossesBorder(BaggageOption baggage) =>
        ChangesAirport || (Kind == ConnectionKind.SelfTransfer && baggage == BaggageOption.CheckedBaggages);
}
