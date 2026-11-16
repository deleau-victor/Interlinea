namespace Domain.Search.Offers.AncillaryCosts;

/// <summary>
/// A cost that the traveler will pay in addition to the ticket price
/// </summary>
/// <param name="Kind">Kind of the cost</param>
/// <param name="Label">The displayable label, for exemple "ESTA (US)"</param>
/// <param name="Amount">Amount converted to the search currency</param>
/// <param name="Original">Amount in it's base currency</param>
public sealed record AncillaryCost(
    AncillaryKind Kind,
    string Label,
    Money Amount,
    Money Original
);
