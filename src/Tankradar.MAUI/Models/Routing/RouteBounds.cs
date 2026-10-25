namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Der umschließende Kasten einer Route in Grad.
/// </summary>
/// <param name="MinLatitude">Der südlichste Breitengrad.</param>
/// <param name="MinLongitude">Der westlichste Längengrad.</param>
/// <param name="MaxLatitude">Der nördlichste Breitengrad.</param>
/// <param name="MaxLongitude">Der östlichste Längengrad.</param>
/// <returns>Der Wert.</returns>
public readonly record struct RouteBounds(double MinLatitude, double MinLongitude, double MaxLatitude, double MaxLongitude);
