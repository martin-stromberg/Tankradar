namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Die Lage eines Punktes bezogen auf eine Route.
/// </summary>
/// <param name="OffsetKm">Der kürzeste Abstand zur Route in Kilometern.</param>
/// <param name="AlongKm">Die Position entlang der Route (Strecke vom Start bis zum nächstgelegenen Punkt) in Kilometern.</param>
/// <returns>Der Wert.</returns>
public readonly record struct RouteLocation(double OffsetKm, double AlongKm);
