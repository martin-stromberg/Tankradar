namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Die Angaben zu einer Route, die wegen der Abfragegrenze nicht durchsucht wird (vor der ersten Preisabfrage ermittelt).
/// </summary>
/// <param name="DistanceKm">Der gewählte Abstand zur Route in Kilometern.</param>
/// <param name="RouteLengthKm">Die Länge der berechneten Route in Kilometern.</param>
/// <param name="MaxSearchLengthKm">Die Länge, bis zu der bei diesem Abstand gesucht werden kann, in Kilometern.</param>
/// <returns>Der Wert.</returns>
public sealed record RouteLimit(int DistanceKm, double RouteLengthKm, double MaxSearchLengthKm);
