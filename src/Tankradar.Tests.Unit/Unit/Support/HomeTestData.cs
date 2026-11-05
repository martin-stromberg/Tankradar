using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Gemeinsame Testdaten der Startseite: Gruppen mit Einträgen und passende Tankstellen.
/// </summary>
public static class HomeTestData
{
    /// <summary>
    /// Erzeugt eine Gruppe mit Einträgen für die angegebenen Tankstellen (Kennung und Name aus <see cref="StationFactory"/>).
    /// </summary>
    /// <param name="id">Die Kennung der Gruppe.</param>
    /// <param name="name">Der Name der Gruppe.</param>
    /// <param name="stations">Die Tankstellen in der Reihenfolge der Gruppe.</param>
    /// <returns>Die Gruppe mit Einträgen.</returns>
    public static FavoriteGroupOverview Group(long id, string name, params StationInfo[] stations)
    {
        var entries = stations.Select(station => new FavoriteEntry(id, station.Id, station.Name, string.Empty, null, FavoritePriority.None)).ToList();
        return new FavoriteGroupOverview(new FavoriteGroup(id, name, null, entries.Count), entries);
    }

    /// <summary>
    /// Erzeugt ein Wörterbuch der Tankstellen je Kennung.
    /// </summary>
    /// <param name="stations">Die Tankstellen.</param>
    /// <returns>Das Wörterbuch.</returns>
    public static Dictionary<string, StationInfo> ById(params StationInfo[] stations)
    {
        return stations.ToDictionary(station => station.Id);
    }

    /// <summary>
    /// Erzeugt eine Tankstelle mit Standardpreisen (E5 und Diesel) zum angegebenen Abrufzeitpunkt.
    /// </summary>
    /// <param name="number">Laufende Nummer (bestimmt Kennung und Lage).</param>
    /// <param name="retrievedUtc">Der Abrufzeitpunkt der Preise.</param>
    /// <returns>Die Tankstelle.</returns>
    public static StationInfo Station(int number, DateTime retrievedUtc)
    {
        return StationFactory.Create(number, retrievedUtc, (FuelType.SuperE5, 1.859m), (FuelType.Diesel, 1.699m));
    }
}
