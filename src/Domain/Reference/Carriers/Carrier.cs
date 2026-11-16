namespace Domain.Reference.Carriers;

/// <summary>
/// Represents an airline carrier.
/// </summary>
/// <param name="Code">The unique carrier IATA identifier code</param>
/// <param name="Name">The commercial name of the carrier</param>
/// <param name="Model">The core business model of the carrier</param>
public sealed record Carrier(CarrierCode Code, string Name, CarrierModel Model);
