namespace Domain.Reference.Carriers;

/// <summary>
/// Defines the core business models of airline carriers.
/// Each model dictates operational strategies, fleet composition, and network structures.
/// </summary>
public enum CarrierModel
{
    /// <summary>
    /// Traditional full-service network carriers (FSNCs) with Hub & Spoke model.
    /// Examples: Air France-KLM (AF/KL), Lufthansa (LH), Delta Air Lines (DL), British Airways (BA).
    /// </summary>
    Legacy,

    /// <summary>
    /// Megahub-and-spoke carriers to connect distant global regions.
    /// They rely on a single massive hub and a predominantly wide-body fleet to offer one-stop global connectivity.
    /// Examples: Emirates (DXB), Qatar Airways (DOH), Turkish Airlines (IST), Etihad Airways (AUH).
    /// </summary>
    SuperConnector,

    /// <summary>
    /// Low-Cost Carriers (LCCs) and Ultra Low-Cost Carriers (ULCCs) operating point-to-point networks.
    /// They focus on cost minimization, secondary airports with fewer cost, single-type short/medium-haul fleets, and ancillary revenues.
    /// Examples: Ryanair (FR), EasyJet (U2), Southwest Airlines (WN), Wizz Air (W6).
    /// </summary>
    LowCost,

    /// <summary>
    /// Hybrid companies, between network and low-cost.
    /// They may offer point-to-point routes with low fares, while also providing bundled services.
    /// Examples: JetBlue (B6), FlyDubai (FZ), Azul Brazilian Airlines. (AD)
    /// </summary>
    Hybrid,

    /// <summary>
    /// Charter and holiday-focused carriers prioritizing leisure destinations
    /// Characterized by highly seasonal flight schedules, dense high-capacity cabin configurations.
    /// Examples: Air Transat (TS), TUI Airways (BY), Condor (DE), SunExpress (XQ).
    /// </summary>
    Leisure,

    /// <summary>
    /// VFR (Visiting Friends and Relatives) targeting specific expatriate, immigrant, or diaspora communities.
    /// They frequently serve routes connecting major cultural hubs to regions with lower tourism but heavy community-driven demand.
    /// Examples: Plus Ultra (PU), Volaris (Y4), Air Austral (UU).
    /// </summary>
    Vfr,

    /// <summary>
    /// Niche carriers operating exclusive, high-end services, usually featuring all-business or premium-only cabin configurations.
    /// Examples: La Compagnie a All-Business Paris-NY (B0), BeOnd (B4).
    /// </summary>
    Boutique,

    /// <summary>
    /// Short-haul carriers operating smaller turboprop or regional jet aircraft (e.g., Embraer, ATR).
    /// Examples: SkyWest Airlines (OO), Air Nostrum (YW), HOP! (A5), Loganair (LM).
    /// </summary>
    Regional,
}
