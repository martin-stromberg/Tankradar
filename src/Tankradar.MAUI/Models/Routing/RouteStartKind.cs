namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Die Art des Startpunkts einer Routensuche.
/// </summary>
public enum RouteStartKind
{
    /// <summary>
    /// Der aktuelle Standort (benötigt die Standortnutzung).
    /// </summary>
    CurrentLocation,

    /// <summary>
    /// Eine eingegebene Adresse, ein Ort oder eine Postleitzahl.
    /// </summary>
    Address,
}
