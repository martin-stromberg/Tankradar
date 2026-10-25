namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Ergebnis der Prüfung eines OpenRouteService-Schlüssels per kleiner Testanfrage.
/// </summary>
public enum RoutingKeyCheckResult
{
    /// <summary>
    /// Der Dienst hat den Schlüssel akzeptiert.
    /// </summary>
    Valid,

    /// <summary>
    /// Der Dienst hat den Schlüssel abgelehnt.
    /// </summary>
    Invalid,

    /// <summary>
    /// Der Dienst war nicht erreichbar (Verbindungsfehler, Zeitüberschreitung, Serverfehler).
    /// </summary>
    Unreachable,

    /// <summary>
    /// Das Kontingent des Schlüssels ist aufgebraucht oder der Dienst ist ausgelastet.
    /// </summary>
    QuotaExceeded,

    /// <summary>
    /// Die Prüfung ist nicht möglich (kein Endpunkt, abgelehnte Anfrage oder unerwartete Antwort).
    /// </summary>
    Unavailable,
}
