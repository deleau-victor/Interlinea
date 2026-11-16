namespace Domain.Fares;

/// <summary>
/// A seller of a fare.
/// </summary>
/// <param name="Name">The name of the seller</param>
/// <param name="Kind">The kind of seller</param>
public sealed record Seller(string Name, SellerKind Kind);
