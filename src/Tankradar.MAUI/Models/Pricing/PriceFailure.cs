namespace Tankradar.MAUI.Models.Pricing;

/// <summary>
/// Grund, warum ein Abruf bei der Preis-API nicht erfolgreich war.
/// </summary>
public enum PriceFailure
{
    /// <summary>
    /// Kein Fehler.
    /// </summary>
    None,

    /// <summary>
    /// Das Gerät hat laut Betriebssystem keine Netzverbindung.
    /// </summary>
    Offline,

    /// <summary>
    /// Der Preisdienst war trotz Wiederholungen nicht erreichbar (Verbindungsfehler, Zeitüberschreitung, Serverfehler).
    /// </summary>
    Unreachable,

    /// <summary>
    /// Es ist kein API-Schlüssel hinterlegt.
    /// </summary>
    ApiKeyMissing,

    /// <summary>
    /// Der Preisdienst hat die Anfrage abgelehnt (z. B. ungültiger Schlüssel oder Nutzungsgrenze).
    /// </summary>
    Rejected,

    /// <summary>
    /// Die Antwort des Preisdienstes war nicht auswertbar.
    /// </summary>
    InvalidResponse,
}
