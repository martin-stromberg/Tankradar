namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Der Fortschritt einer laufenden Routensuche.
/// </summary>
/// <param name="Phase">Der aktuelle Abschnitt.</param>
/// <param name="Current">Bei <see cref="RouteSearchPhase.SearchingStations"/> die laufende Nummer der Abfrage, sonst 0.</param>
/// <param name="Total">Bei <see cref="RouteSearchPhase.SearchingStations"/> die Gesamtzahl der Abfragen, sonst 0.</param>
/// <returns>Der Wert.</returns>
public sealed record RouteSearchProgress(RouteSearchPhase Phase, int Current, int Total);
