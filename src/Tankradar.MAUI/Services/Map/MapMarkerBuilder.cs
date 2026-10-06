using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Erzeugt die Markierungen der Karte aus der aufbereiteten Ergebnismenge: Preis der maßgeblichen Spritsorte, Preisniveau und Beschreibung.
/// Tankstellen ohne gültige Position erscheinen nicht auf der Karte.
/// </summary>
public static class MapMarkerBuilder
{
    /// <summary>
    /// Erzeugt die Markierungen.
    /// </summary>
    /// <param name="stations">Die Tankstellen der aktuellen Ergebnismenge (nach Filter; alle Seiten, nicht nur die dargestellte Listenseite).</param>
    /// <param name="fuelType">Die maßgebliche Spritsorte; <see langword="null"/>, wenn keine Sorte ausgewählt ist (dann ohne Preise).</param>
    /// <returns>Die Markierungen in der Reihenfolge der Tankstellen.</returns>
    public static IReadOnlyList<MapMarker> Build(IReadOnlyList<StationListItem> stations, FuelType? fuelType)
    {
        ArgumentNullException.ThrowIfNull(stations);

        var withPosition = stations.Where(station => station.HasPosition).ToList();
        var lines = withPosition.Select(station => fuelType is { } fuel ? station.PriceLines.FirstOrDefault(line => line.FuelType == fuel) : null).ToList();
        var levels = PriceLevelClassifier.Classify(
            withPosition.Select((station, index) => new PriceLevelInput(lines[index]?.Price, station.IsOpen == false)).ToList());

        var fuelLabel = fuelType is { } selected ? SettingsTexts.GetLabel(selected) : string.Empty;
        var markers = new List<MapMarker>(withPosition.Count);
        for (var index = 0; index < withPosition.Count; index++)
        {
            var station = withPosition[index];
            var line = lines[index];
            markers.Add(new MapMarker(
                station,
                station.Latitude!.Value,
                station.Longitude!.Value,
                levels[index],
                line?.PriceText ?? string.Empty,
                MapTexts.FormatMarkerDescription(station.Name, fuelLabel, line?.PriceSpokenText ?? string.Empty)));
        }

        return markers;
    }
}
