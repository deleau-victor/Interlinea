using Domain.Search.Offers;

namespace Domain.Search.Ranking;

/// <summary>
/// An offer and it's score, the lower it is, better is the offer
/// </summary>
/// <param name="Offer">The offer</param>
/// <param name="Score">The weights of each inconvenient</param>
public sealed record RankedOffer(Offer Offer, decimal Score);
