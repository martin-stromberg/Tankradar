using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.Services.Search;

/// <summary>
/// Bereitet die Daten einer Tankstelle für die Detailansicht auf: Preiszeilen der aktivierten Sorten in der Reihenfolge der Einstellungen mit Altersangabe,
/// Hinweise, Öffnungszeiten (nur bis zur Altersgrenze der Detailangaben). Fehlende Quelldaten werden ausgeblendet und nie durch Platzhalter ersetzt.
/// </summary>
public static class StationDetailBuilder
{
    /// <summary>
    /// Erzeugt die Detailangaben.
    /// </summary>
    /// <param name="station">Die Tankstelle mit den zuletzt bekannten Daten.</param>
    /// <param name="fuelTypes">Die Spritsorten der Einstellungen in ihrer Reihenfolge; nur ausgewählte Sorten werden angezeigt.</param>
    /// <param name="distanceKm">Die Entfernung aus der Suche; <see langword="null"/>, wenn unbekannt (die Quelle liefert sie in der Detailabfrage nicht).</param>
    /// <param name="nowUtc">Der aktuelle Zeitpunkt in UTC (für Alter und Hinweise).</param>
    /// <returns>Die Detailangaben.</returns>
    public static StationDetailItem Build(StationInfo station, IReadOnlyList<FuelTypeSelection> fuelTypes, double? distanceKm, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(station);
        ArgumentNullException.ThrowIfNull(fuelTypes);

        // Öffnungszeiten und davon abgeleitete Hinweise nur, solange die Detailangaben nicht älter als die Altersgrenze sind.
        var current = station.WithoutStaleDetails(nowUtc);

        var selected = fuelTypes
            .Where(selection => selection.IsSelected && Enum.IsDefined(selection.FuelType))
            .Select(selection => selection.FuelType)
            .Distinct()
            .ToList();
        var shownPrices = new List<FuelPrice>();
        var lines = new List<StationPriceLine>();
        foreach (var fuel in selected)
        {
            var price = current.Prices.FirstOrDefault(candidate => candidate.FuelType == fuel);
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
                PriceFreshness.IsStale(price.RetrievedUtc, nowUtc)));
        }

        var hours = current.OpeningTimes.Select(FormatOpeningHours).Where(line => line.DisplayText.Length > 0).ToList();
        var ageText = hours.Count > 0 && current.DetailsUpdatedUtc is { } updated
            ? DetailTexts.FormatOpeningHoursAge(DetailFreshness.FormatAge(updated, nowUtc))
            : string.Empty;

        return new StationDetailItem(
            current.Id,
            current.Name,
            FormatBrand(current),
            SearchTexts.FormatAddress(current.Street, current.HouseNumber, current.PostCode, current.Place),
            distanceKm is { } distance ? SearchTexts.FormatDistance(distance) : string.Empty,
            current.IsOpen is { } isOpen ? (isOpen ? SearchTexts.Open : SearchTexts.Closed) : string.Empty,
            lines,
            StationHints.HasUnconfirmedPrice(shownPrices, nowUtc),
            StationHints.IsAutomatedStation(current.WholeDay, current.OpeningTimes),
            hours,
            ageText);
    }

    private static string FormatBrand(StationInfo station)
    {
        var brand = station.Brand?.Trim() ?? string.Empty;
        return string.Equals(brand, station.Name.Trim(), StringComparison.OrdinalIgnoreCase) ? string.Empty : brand;
    }

    private static OpeningHoursLine FormatOpeningHours(OpeningTimeEntry entry)
    {
        var day = entry.Text?.Trim() ?? string.Empty;
        var start = ShortTime(entry.Start);
        var end = ShortTime(entry.End);
        var time = start.Length > 0 && end.Length > 0 ? DetailTexts.FormatTimeRange(start, end) : string.Empty;
        var display = string.Join(": ", new[] { day, time }.Where(part => part.Length > 0));
        return new OpeningHoursLine(day, time, display);
    }

    private static string ShortTime(string? value)
    {
        // Die Quelle liefert „HH:mm:ss“; angezeigt wird „HH:mm“.
        var text = value?.Trim() ?? string.Empty;
        return text.Length >= 5 && text[2] == ':' ? text[..5] : text;
    }
}
