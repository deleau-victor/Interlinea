using Domain.Fares;

namespace Domain.Search.Offers;

/// <summary>
/// The retained ticket for an offer, ans the others sellers proposing the same tickets
/// </summary>
/// <param name="Selected">The cheapest ticket</param>
/// <param name="Alternatives">The same tickets from others sellers</param>
public sealed record OfferedTicket(Ticket Selected, IReadOnlyList<Ticket> Alternatives);
