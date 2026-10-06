using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Models.Map;

/// <summary>
/// Eine Tankstellenmarkierung auf der Karte.
/// </summary>
/// <param name="Station">Die Tankstelle (wird beim Antippen an die Detailansicht übergeben).</param>
/// <param name="Latitude">Der Breitengrad der Tankstelle.</param>
/// <param name="Longitude">Der Längengrad der Tankstelle.</param>
/// <param name="Level">Das Preisniveau (bestimmt die Farbe).</param>
/// <param name="PriceText">Der Preis der maßgeblichen Sorte („1,859 €“); leer, wenn die Tankstelle diese Sorte nicht führt.</param>
/// <param name="Description">Die Beschreibung für Bedienhilfen („Name, Super E5 1,859 Euro pro Liter“).</param>
/// <returns>Der Wert.</returns>
public sealed record MapMarker(StationListItem Station, double Latitude, double Longitude, PriceLevel Level, string PriceText, string Description)
{
    /// <summary>
    /// Gibt an, ob ein Preis angezeigt wird.
    /// </summary>
    public bool HasPrice => PriceText.Length > 0;
}
