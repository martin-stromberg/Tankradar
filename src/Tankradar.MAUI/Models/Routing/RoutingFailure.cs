namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Grund, warum eine Routenberechnung beim Routingdienst nicht erfolgreich war.
/// </summary>
public enum RoutingFailure
{
    /// <summary>
    /// Kein Fehler.
    /// </summary>
    None,

    /// <summary>
    /// Es ist kein Schlüssel für den Routingdienst hinterlegt.
    /// </summary>
    ApiKeyMissing,

    /// <summary>
    /// Im Testmodus ist kein Endpunkt für den Routingdienst angegeben; der Abruf wird verweigert (kein Rückfall auf den produktiven Dienst).
    /// </summary>
    EndpointNotConfigured,

    /// <summary>
    /// Der Routingdienst hat den Schlüssel abgelehnt.
    /// </summary>
    KeyRejected,

    /// <summary>
    /// Der Routingdienst war trotz Wiederholungen nicht erreichbar (Verbindungsfehler, Zeitüberschreitung, Serverfehler).
    /// </summary>
    Unreachable,

    /// <summary>
    /// Der Routingdienst ist ausgelastet oder das Kontingent ist aufgebraucht.
    /// </summary>
    QuotaExceeded,

    /// <summary>
    /// Zwischen Start und Ziel konnte keine Route gefunden werden.
    /// </summary>
    RouteNotFound,

    /// <summary>
    /// Die Route überschreitet die Streckenbegrenzung des Routingdienstes.
    /// </summary>
    RouteTooLong,

    /// <summary>
    /// Der Routingdienst hat die Anfrage aus einem anderen Grund abgelehnt.
    /// </summary>
    Rejected,

    /// <summary>
    /// Die Antwort des Routingdienstes war nicht auswertbar.
    /// </summary>
    InvalidResponse,
}
