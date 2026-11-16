namespace Domain.Search.Planning.Partitions;

/// <summary>A partition plan ans its estimate floor price.</summary>
/// <param name="Plan">The partition and its anchor</param>
/// <param name="Floor">The sum of the observed floors an the both sides</param>
public sealed record PartitionEstimate(PartitionPlan Plan, Money Floor);
