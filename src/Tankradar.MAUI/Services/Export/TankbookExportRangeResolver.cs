using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Export;

/// <summary>
/// Löst die Voreinstellungen des Zeitraums eines Exports in Start- und Endtag auf (lokale Wanduhrzeit, beide Tage einschließlich).
/// </summary>
public static class TankbookExportRangeResolver
{
    /// <summary>
    /// Prüft einen Zeitraum: Der Starttag darf nicht nach dem Endtag liegen, der Endtag nicht in der Zukunft.
    /// </summary>
    /// <param name="from">Der Starttag; <see langword="null"/> ohne untere Grenze.</param>
    /// <param name="to">Der Endtag; <see langword="null"/> ohne obere Grenze.</param>
    /// <param name="today">Der heutige Tag (lokale Wanduhrzeit).</param>
    /// <returns>Der Fehler oder <see cref="TankbookExportRangeError.None"/>.</returns>
    public static TankbookExportRangeError Validate(DateTime? from, DateTime? to, DateTime today)
    {
        if (from is { } start && to is { } end && start.Date > end.Date)
        {
            return TankbookExportRangeError.StartAfterEnd;
        }

        return to is { } last && last.Date > today.Date ? TankbookExportRangeError.EndInFuture : TankbookExportRangeError.None;
    }

    /// <summary>
    /// Ermittelt Start- und Endtag eines Zeitraums.
    /// </summary>
    /// <param name="kind">Die Art des Zeitraums.</param>
    /// <param name="today">Der heutige Tag (lokale Wanduhrzeit).</param>
    /// <param name="customFrom">Der Starttag bei <see cref="TankbookExportRangeKind.Custom"/>.</param>
    /// <param name="customTo">Der Endtag bei <see cref="TankbookExportRangeKind.Custom"/>.</param>
    /// <returns>Der Zeitraum mit Start- und Endtag (jeweils ohne Uhrzeit); <see langword="null"/> bedeutet keine Grenze.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Die Art ist kein bekannter Wert.</exception>
    public static TankbookExportRange Resolve(TankbookExportRangeKind kind, DateTime today, DateTime? customFrom = null, DateTime? customTo = null)
    {
        var day = today.Date;
        return kind switch
        {
            TankbookExportRangeKind.All => new TankbookExportRange(null, null),
            TankbookExportRangeKind.ThisYear => new TankbookExportRange(new DateTime(day.Year, 1, 1), day),
            TankbookExportRangeKind.LastYear => new TankbookExportRange(new DateTime(day.Year - 1, 1, 1), new DateTime(day.Year - 1, 12, 31)),
            TankbookExportRangeKind.Last12Months => new TankbookExportRange(day.AddMonths(-12).AddDays(1), day),
            TankbookExportRangeKind.Custom => new TankbookExportRange(customFrom?.Date, customTo?.Date),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unbekannte Art des Zeitraums."),
        };
    }
}
