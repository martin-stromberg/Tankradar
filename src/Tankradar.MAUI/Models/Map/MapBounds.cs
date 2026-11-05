namespace Tankradar.MAUI.Models.Map;

/// <summary>
/// Die Begrenzung des sichtbaren Kartenausschnitts in Breite und Länge. Sie wird nur im Arbeitsspeicher gehalten (nie gespeichert oder protokolliert).
/// </summary>
/// <param name="MinLatitude">Der südlichste Breitengrad.</param>
/// <param name="MinLongitude">Der westlichste Längengrad.</param>
/// <param name="MaxLatitude">Der nördlichste Breitengrad.</param>
/// <param name="MaxLongitude">Der östlichste Längengrad.</param>
/// <returns>Der Wert.</returns>
public readonly record struct MapBounds(double MinLatitude, double MinLongitude, double MaxLatitude, double MaxLongitude)
{
    /// <summary>
    /// Gibt eine Darstellung ohne Koordinaten zurück, damit der Ausschnitt nicht versehentlich protokolliert wird.
    /// </summary>
    /// <returns>Ein fester Text.</returns>
    public override string ToString()
    {
        return nameof(MapBounds);
    }
}
