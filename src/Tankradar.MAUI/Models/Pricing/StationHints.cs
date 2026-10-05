namespace Tankradar.MAUI.Models.Pricing;

/// <summary>
/// Leitet Zusatzhinweise zu einer Tankstelle aus den vorhandenen Quelldaten ab.
/// </summary>
public static class StationHints
{
    /// <summary>
    /// Prüft, ob die Tankstelle als Automatentankstelle gelten kann, weil sie durchgehend geöffnet ist.
    /// Nur ein positives Ergebnis wird angezeigt; unbekannte Öffnungszeiten ergeben <see langword="false"/>.
    /// </summary>
    /// <param name="wholeDay">Die Angabe der Quelle „durchgehend geöffnet“, sofern geliefert.</param>
    /// <param name="openingTimes">Die Öffnungszeiten der Quelle.</param>
    /// <returns><see langword="true"/>, wenn die Öffnungszeiten durchgehende Öffnung belegen.</returns>
    public static bool IsAutomatedStation(bool? wholeDay, IReadOnlyList<OpeningTimeEntry> openingTimes)
    {
        ArgumentNullException.ThrowIfNull(openingTimes);

        if (wholeDay is { } explicitValue)
        {
            return explicitValue;
        }

        return openingTimes.Count > 0 && openingTimes.All(IsAroundTheClock);
    }

    /// <summary>
    /// Prüft, ob mindestens ein Preis veraltet ist und die Tankstelle daher den Hinweis „Preis unbestätigt“ erhält.
    /// </summary>
    /// <param name="prices">Die Preise der Tankstelle.</param>
    /// <param name="nowUtc">Aktueller Zeitpunkt in UTC.</param>
    /// <returns><see langword="true"/>, wenn mindestens ein Preis mindestens 60 Minuten alt ist.</returns>
    public static bool HasUnconfirmedPrice(IEnumerable<FuelPrice> prices, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(prices);

        return prices.Any(price => PriceFreshness.IsStale(price.RetrievedUtc, nowUtc));
    }

    private static bool IsAroundTheClock(OpeningTimeEntry entry)
    {
        return entry.Start == "00:00:00" && (entry.End == "24:00:00" || entry.End == "23:59:59");
    }
}
