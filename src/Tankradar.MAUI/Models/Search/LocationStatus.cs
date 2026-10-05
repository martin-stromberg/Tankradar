namespace Tankradar.MAUI.Models.Search;

/// <summary>
/// Ergebnis einer Standortabfrage.
/// </summary>
public enum LocationStatus
{
    /// <summary>
    /// Der Standort wurde ermittelt.
    /// </summary>
    Available,

    /// <summary>
    /// Die Einstellung „Nie“ verbietet die Standortnutzung; es wurde nichts abgefragt.
    /// </summary>
    DisabledBySetting,

    /// <summary>
    /// Der Anwender hat die Berechtigung verweigert.
    /// </summary>
    PermissionDenied,

    /// <summary>
    /// Der Standort ist nicht ermittelbar (nicht verfügbar, Zeitüberschreitung, Fehler).
    /// </summary>
    Unavailable,
}
