namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Der Abschnitt einer laufenden Routensuche (für die Fortschrittsanzeige).
/// </summary>
public enum RouteSearchPhase
{
    /// <summary>
    /// Start und Ziel werden aufgelöst.
    /// </summary>
    ResolvingPlaces,

    /// <summary>
    /// Die Route wird berechnet.
    /// </summary>
    CalculatingRoute,

    /// <summary>
    /// Die Tankstellen entlang der Route werden abgefragt.
    /// </summary>
    SearchingStations,
}
