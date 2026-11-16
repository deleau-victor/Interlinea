using Domain.Search.Offers;

namespace Domain.Search.Ranking;

/// <summary>
/// Rank the offers
/// </summary>
/// <param name="weights">The weights of each inconvenient</param>
public sealed class Ranker(RankingWeights weights)
{
    /// <summary>
    /// Rank the offer, from the better to the worst, at equal score, the lower price first
    /// </summary>
    /// <param name="offers">The offers to rank</param>
    /// <returns>The ranked offers</returns>
    public IReadOnlyList<RankedOffer> Rank(IEnumerable<Offer> offers) =>
        [.. offers
            .Select(offer => new RankedOffer(offer, Score(offer)))
            .OrderBy(ranked => ranked.Score)
            .ThenBy(ranked => ranked.Offer.TotalCost)
        ];

    /// <summary>
    /// Get the weighted score of an offer
    /// </summary>
    /// <param name="offer">The offer</param>
    /// <returns>The score of the offer</returns>
    public decimal Score(Offer offer) =>
        offer.TotalCost.Amount
        + (weights.PerHour * (decimal)(offer.OutboundDuration + offer.InboundDuration).TotalHours)
        + (weights.PerStop * offer.Stops)
        + (weights.PerSelfTransfer * offer.SelfTransfers);
}
