namespace Domain.Search.Planning.Partitions;

/// <summary>
/// The results of the partitions selection for a research
/// </summary>
/// <param name="Retained">Partitions to explore, ordered by pertinence</param>
/// <param name="ToProbe">Hubs without recent profile, to probe before exclude or retain</param>
/// <param name="Pruned">Pruned partitions, their floor price are already upper than the best known regular offer</param>
public sealed record PartitionSelection(
    IReadOnlyList<PartitionEstimate> Retained,
    IReadOnlyList<AirportCode> ToProbe,
    IReadOnlyList<PartitionEstimate> Pruned);
