namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Der Ausgang einer Routensuche.
/// </summary>
public enum RouteSearchStatus
{
    /// <summary>
    /// Die Suche war erfolgreich (die Trefferliste kann leer sein).
    /// </summary>
    Success,

    /// <summary>
    /// Zum Start wurde kein Ort gefunden.
    /// </summary>
    StartNotFound,

    /// <summary>
    /// Zum Ziel wurde kein Ort gefunden.
    /// </summary>
    DestinationNotFound,

    /// <summary>
    /// Die Ortsauflösung ist aus einem anderen Grund fehlgeschlagen.
    /// </summary>
    GeocodingFailed,

    /// <summary>
    /// Start und Ziel liegen am selben Ort.
    /// </summary>
    SamePlace,

    /// <summary>
    /// Der Routingdienst ist nicht verfügbar (z. B. fehlender Schlüssel).
    /// </summary>
    RoutingUnavailable,

    /// <summary>
    /// Die Route konnte nicht berechnet werden.
    /// </summary>
    RoutingFailed,

    /// <summary>
    /// Die Route ist für die Suche zu lang.
    /// </summary>
    RouteTooLong,

    /// <summary>
    /// Der Abruf der Tankstellen ist fehlgeschlagen.
    /// </summary>
    PriceFailed,
}
