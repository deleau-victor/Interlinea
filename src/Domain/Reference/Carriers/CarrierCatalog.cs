namespace Domain.Reference.Carriers;

/// <summary>
/// Catalog providing read-only access to airline carriers.
/// </summary>
public sealed class CarrierCatalog(IEnumerable<Carrier> carriers)
{
    private readonly FrozenDictionary<CarrierCode, Carrier> _byCode = carriers.ToFrozenDictionary(c => c.Code);

    /// <summary>
    /// Retrieves a carrier by its unique IATA code.
    /// </summary>
    /// <param name="code">The IATA code to look up.</param>
    /// <returns>The matching <see cref="Carrier"/> instance.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the code does not exist in the catalog.</exception>
    public Carrier Get(CarrierCode code) =>
        _byCode.TryGetValue(code, out Carrier? carrier)
            ? carrier
            : throw new KeyNotFoundException($"Carrier missing from the reference data : {code}.");

    /// <summary>
    /// Safely attempts to retrieve a carrier by its unique IATA code without throwing an exception.
    /// </summary>
    /// <param name="code">The IATA code to look up.</param>
    /// <param name="carrier">When this method returns, contains the matching <see cref="Carrier"/> if found; otherwise, null.</param>
    /// <returns><see langword="true"/> if the carrier was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGet(CarrierCode code, [NotNullWhen(true)] out Carrier? carrier) => _byCode.TryGetValue(code, out carrier);
}

