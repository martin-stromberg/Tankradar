namespace Tankradar.MAUI.Models.Pricing;

/// <summary>
/// Herkunft der gelieferten Preisdaten.
/// </summary>
public enum PriceDataSource
{
    /// <summary>
    /// Frisch von der Preis-API abgerufen.
    /// </summary>
    Live,

    /// <summary>
    /// Aus dem lokalen Cache, weil dessen Daten noch jung genug sind (kein erneuter Abruf nötig).
    /// </summary>
    Cache,

    /// <summary>
    /// Zuletzt bekannte Werte aus dem lokalen Speicher, weil ein Abruf nicht möglich war (Offline-Betrieb); die Preise tragen ihr Alter.
    /// </summary>
    OfflineFallback,
}
