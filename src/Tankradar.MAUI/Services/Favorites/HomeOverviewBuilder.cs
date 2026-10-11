using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Search;

namespace Tankradar.MAUI.Services.Favorites;

/// <summary>
/// Bereitet die Gruppenübersicht der Startseite auf: je Gruppe die Tankstellen mit Preiszeilen der gewählten Sorten, Alter, Hinweisen und
/// – nur wenn ein Standort vorliegt – der Entfernung. Fehlende Angaben werden ausgeblendet und nie durch Platzhalter ersetzt.
/// </summary>
public static class HomeOverviewBuilder
{
    /// <summary>
    /// Erzeugt die Gruppen der Startseite.
    /// </summary>
    /// <param name="overview">Die Gruppen mit Einträgen in der Reihenfolge des Dienstes.</param>
    /// <param name="stations">Die lokal bekannten Tankstellen je Kennung.</param>
    /// <param name="fuelTypes">Die Spritsorten der Einstellungen in ihrer Reihenfolge; nur ausgewählte Sorten werden angezeigt.</param>
    /// <param name="position">Der aktuelle Standort; <see langword="null"/>, wenn keiner ermittelt werden darf oder konnte (dann ohne Entfernungen).</param>
    /// <param name="nowUtc">Der aktuelle Zeitpunkt in UTC (für Alter und Hinweise).</param>
    /// <returns>Die Gruppen der Startseite.</returns>
    public static IReadOnlyList<HomeGroup> Build(
        IReadOnlyList<FavoriteGroupOverview> overview,
        IReadOnlyDictionary<string, StationInfo> stations,
        IReadOnlyList<FuelTypeSelection> fuelTypes,
        GeoPosition? position,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(overview);
        ArgumentNullException.ThrowIfNull(stations);
        ArgumentNullException.ThrowIfNull(fuelTypes);

        return overview
            .Select(entry => new HomeGroup(
                entry.Group.Id,
                entry.Group.Name,
                entry.Group.Description,
                FavoritesTexts.FormatStationCount(entry.Entries.Count),
                entry.Entries.Select(favorite => BuildStation(favorite, stations, fuelTypes, position, nowUtc)).ToList()))
            .ToList();
    }

    private static HomeStation BuildStation(
        FavoriteEntry favorite,
        IReadOnlyDictionary<string, StationInfo> stations,
        IReadOnlyList<FuelTypeSelection> fuelTypes,
        GeoPosition? position,
        DateTime nowUtc)
    {
        if (!stations.TryGetValue(favorite.StationId, out var station))
        {
            var unknown = new StationListItem(favorite.StationId, favorite.StationName, null, string.Empty, [], false, false, string.Empty, favorite.AddressText);
            return new HomeStation(unknown, favorite.Note, favorite.Priority);
        }

        var withDistance = station.WithDistance(DistanceTo(station, position));
        return new HomeStation(StationResultBuilder.BuildItem(withDistance, fuelTypes, nowUtc), favorite.Note, favorite.Priority);
    }

    private static double? DistanceTo(StationInfo station, GeoPosition? position)
    {
        // (0, 0) ist der Standardwert fehlender Angaben und liegt nie in Deutschland.
        if (position is null || IsOrigin(position.Latitude, position.Longitude) || IsOrigin(station.Latitude, station.Longitude))
        {
            return null;
        }

        return PriceRepository.DistanceKm(position.Latitude, position.Longitude, station.Latitude, station.Longitude);
    }

    private static bool IsOrigin(double latitude, double longitude)
    {
        return latitude == 0 && longitude == 0;
    }
}
