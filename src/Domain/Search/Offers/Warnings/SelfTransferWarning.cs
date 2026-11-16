namespace Domain.Search.Offers.Warnings;

/// <summary>
/// The offer includes self-transfers: display it along with a recommendation to purchase missed-connection insurance.
/// </summary>
/// <param name="Count">The numbers of self-transfers</param>
public sealed record SelfTransferWarning(int Count) : OfferWarning;
