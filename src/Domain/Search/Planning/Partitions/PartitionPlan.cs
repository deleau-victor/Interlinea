namespace Domain.Search.Planning.Partitions;

/// <summary>
/// A ready to use partition, with an anchor.
/// The anchor is necessary to know which side to search first.
/// </summary>
/// <param name="Partition">The cut trip into 2 side</param>
/// <param name="AnchorSide">The first searched side</param>
public sealed record PartitionPlan(Partition Partition, TripSide AnchorSide)
{
    /// <summary>
    /// Gets the second side to search
    /// </summary>
    public TripSide OtherSide => AnchorSide == TripSide.Origin
        ? TripSide.Destination
        : TripSide.Origin;

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Partition}, anchored in {(AnchorSide == TripSide.Origin ? "origin" : "destination")} side";
}
