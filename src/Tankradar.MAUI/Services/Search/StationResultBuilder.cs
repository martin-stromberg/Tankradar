using System.Globalization;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.Services.Search;

/// <summary>
/// Bereitet das Ergebnis einer Umkreissuche für die Liste auf: Filter nach Spritsorte, Preiszeilen in der Reihenfolge der Einstellungen,
/// Altersangaben, Hinweise und Sortierung. Fehlende Quelldaten werden ausgeblendet und nie durch Platzhalter ersetzt.
/// </summary>
public static class StationResultBuilder
{
    private static readonly StringComparer NameComparer = StringComparer.Create(CultureInfo.GetCultureInfo("de-DE"), ignoreCase: true);

    /// <summary>
    /// Erzeugt die Ergebnisliste.
    /// </summary>
    /// <param name="stations">Die Tankstellen der Suche.</param>
    /// <param name="fuelTypes">Die Spritsorten der Einstellungen in ihrer Reihenfolge; nur ausgewählte Sorten werden angezeigt.</param>
    /// <param name="filter">Die Sorte, nach der gefiltert wird; <see langword="null"/> für „Alle“. Undefinierte oder nicht ausgewählte Sorten gelten als „Alle“.</param>
    /// <param name="sortOrder">Die Sortierung; ein undefinierter Wert sortiert nach Preis.</param>
    /// <param name="nowUtc">Der aktuelle Zeitpunkt in UTC (für Alter und Hinweise).</param>
    /// <returns>Die sortierte Liste.</returns>
    public static IReadOnlyList<StationListItem> Build(
        IReadOnlyList<StationInfo> stations,
        IReadOnlyList<FuelTypeSelection> fuelTypes,
        FuelType? filter,
        ResultSortOrder sortOrder,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(stations);
        ArgumentNullException.ThrowIfNull(fuelTypes);

        var selected = fuelTypes
            .Where(selection => selection.IsSelected && Enum.IsDefined(selection.FuelType))
            .Select(selection => selection.FuelType)
            .Distinct()
            .ToList();
        FuelType? effectiveFilter = filter is { } requested && selected.Contains(requested) ? requested : null;

        var candidates = effectiveFilter is { } fuel
            ? stations.Where(station => station.Prices.Any(price => price.FuelType == fuel))
            : stations;
        var items = candidates.Select(station => CreateItem(station, selected, nowUtc)).ToList();

        var sortFuel = effectiveFilter ?? selected.FirstOrDefault();
        return Sort(items, sortOrder, sortFuel);
    }

    /// <summary>
    /// Ermittelt die maßgebliche Spritsorte für Sortierung und Karte: die gefilterte Sorte, ohne (gültigen) Filter die zuerst gewählte Sorte der Einstellungen.
    /// </summary>
    /// <param name="fuelTypes">Die Spritsorten der Einstellungen in ihrer Reihenfolge.</param>
    /// <param name="filter">Die Sorte, nach der gefiltert wird; <see langword="null"/> für „Alle“.</param>
    /// <returns>Die Sorte; <see langword="null"/>, wenn keine Sorte ausgewählt ist.</returns>
    public static FuelType? ResolveFuelType(IReadOnlyList<FuelTypeSelection> fuelTypes, FuelType? filter)
    {
        ArgumentNullException.ThrowIfNull(fuelTypes);
        var selected = fuelTypes.Where(selection => selection.IsSelected && Enum.IsDefined(selection.FuelType)).Select(selection => selection.FuelType).ToList();
        if (filter is { } requested && selected.Contains(requested))
        {
            return requested;
        }

        return selected.Count > 0 ? selected[0] : null;
    }

    private static StationListItem CreateItem(StationInfo source, IReadOnlyList<FuelType> selected, DateTime nowUtc)
    {
        // Hinweise aus Öffnungszeiten (z. B. „Automatentankstelle“) nur aus Detailangaben, die nicht älter als die Altersgrenze sind.
        var station = source.WithoutStaleDetails(nowUtc);
        var shownPrices = new List<FuelPrice>();
        var lines = new List<StationPriceLine>();
        foreach (var fuel in selected)
        {
            var price = station.Prices.FirstOrDefault(candidate => candidate.FuelType == fuel);
            if (price is null)
            {
                continue;
            }

            shownPrices.Add(price);
            lines.Add(new StationPriceLine(
                fuel,
                SettingsTexts.GetLabel(fuel),
                price.Price,
                SearchTexts.FormatPrice(price.Price),
                PriceFreshness.FormatAge(price.RetrievedUtc, nowUtc),
                PriceFreshness.IsStale(price.RetrievedUtc, nowUtc),
                PriceFreshness.GetAge(price.RetrievedUtc, nowUtc)));
        }

        return new StationListItem(
            station.Id,
            station.Name,
            station.DistanceKm,
            station.DistanceKm is { } distance ? SearchTexts.FormatDistance(distance) : string.Empty,
            lines,
            StationHints.HasUnconfirmedPrice(shownPrices, nowUtc),
            StationHints.IsAutomatedStation(station.WholeDay, station.OpeningTimes),
            station.IsOpen is { } isOpen ? (isOpen ? SearchTexts.Open : SearchTexts.Closed) : string.Empty,
            SearchTexts.FormatAddress(station.Street, station.HouseNumber, station.PostCode, station.Place),
            HasValidPosition(station) ? station.Latitude : null,
            HasValidPosition(station) ? station.Longitude : null,
            station.IsOpen);
    }

    private static bool HasValidPosition(StationInfo station)
    {
        // (0, 0) ist der Standardwert fehlender Angaben und liegt nie in Deutschland.
        return GeoPosition.TryCreate(station.Latitude, station.Longitude, out _) && !(station.Latitude == 0 && station.Longitude == 0);
    }

    private static IReadOnlyList<StationListItem> Sort(List<StationListItem> items, ResultSortOrder sortOrder, FuelType sortFuel)
    {
        return sortOrder switch
        {
            ResultSortOrder.Distance => items
                .OrderBy(item => item.DistanceKm ?? double.MaxValue)
                .ThenBy(item => item.Name, NameComparer)
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .ToList(),
            ResultSortOrder.Name => items
                .OrderBy(item => item.Name, NameComparer)
                .ThenBy(item => item.DistanceKm ?? double.MaxValue)
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .ToList(),
            _ => items
                .OrderBy(item => PriceOf(item, sortFuel) ?? decimal.MaxValue)
                .ThenBy(item => item.DistanceKm ?? double.MaxValue)
                .ThenBy(item => item.Name, NameComparer)
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .ToList(),
        };
    }

    private static decimal? PriceOf(StationListItem item, FuelType fuel)
    {
        return item.PriceLines.FirstOrDefault(line => line.FuelType == fuel)?.Price;
    }
}
