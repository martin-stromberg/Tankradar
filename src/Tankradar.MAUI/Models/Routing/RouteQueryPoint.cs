namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Ein Abfragepunkt entlang der Route: Mittelpunkt und Radius einer Umkreisabfrage beim Preisdienst. Nur im Arbeitsspeicher.
/// </summary>
/// <param name="Latitude">Breitengrad des Mittelpunkts.</param>
/// <param name="Longitude">Längengrad des Mittelpunkts.</param>
/// <param name="RadiusKm">Radius der Abfrage in ganzen Kilometern.</param>
/// <param name="AlongKm">Position des Mittelpunkts entlang der Route in Kilometern.</param>
/// <returns>Der Wert.</returns>
public sealed record RouteQueryPoint(double Latitude, double Longitude, int RadiusKm, double AlongKm)
{
    /// <summary>
    /// Gibt eine Darstellung ohne Koordinaten zurück, damit die Position nicht versehentlich protokolliert wird.
    /// </summary>
    /// <returns>Ein fester Text.</returns>
    public override string ToString()
    {
        return nameof(RouteQueryPoint);
    }
}
