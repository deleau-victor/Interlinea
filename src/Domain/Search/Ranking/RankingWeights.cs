namespace Domain.Search.Ranking;

/// <summary>
/// The cost of each "inconvenient", given in the currency of the research.
/// This weights permit to compare on a valuable ladder
/// </summary>
/// <param name="PerHour">The cost for an hour of trip</param>
/// <param name="PerStop">The cost of a layover</param>
/// <param name="PerSelfTransfer">The additional cost of a self-transfer layover</param>
public sealed record RankingWeights(decimal PerHour, decimal PerStop, decimal PerSelfTransfer);
