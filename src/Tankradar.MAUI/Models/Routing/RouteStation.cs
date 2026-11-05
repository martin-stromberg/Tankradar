using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Eine Tankstelle im Abstandsband einer Route mit ihrer Lage zur Route.
/// </summary>
/// <param name="Station">Die Tankstelle.</param>
/// <param name="AlongKm">Die Position entlang der Route in Kilometern.</param>
/// <param name="OffsetKm">Der Abstand zur Route in Kilometern.</param>
/// <returns>Der Wert.</returns>
public sealed record RouteStation(StationInfo Station, double AlongKm, double OffsetKm);
