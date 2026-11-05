using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Routing;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Einstellungen und Bausteine der Routensuche für Tests der Geometrie und Auswahl: ohne die vorläufige Abfragegrenze und ohne Wartezeit zwischen Abfragen.
/// </summary>
public static class RouteTestOptions
{
    /// <summary>
    /// Erzeugt Einstellungen mit bis zu 15 Abfragen und ohne Mindestabstand (die Grenzen der Auslieferung prüfen eigene Tests).
    /// </summary>
    /// <returns>Die Einstellungen.</returns>
    public static RouteSearchOptions Relaxed()
    {
        return new RouteSearchOptions { MaxQueries = 15, MinQueryInterval = TimeSpan.Zero };
    }

    /// <summary>
    /// Erzeugt einen Taktgeber ohne Wartezeit.
    /// </summary>
    /// <returns>Der Taktgeber.</returns>
    public static RouteQueryPacer NoWaitPacer()
    {
        return new RouteQueryPacer(new RouteSearchOptions { MinQueryInterval = TimeSpan.Zero }, new RecordingDelay(), TimeProvider.System);
    }

    /// <summary>
    /// Erzeugt die Stationssuche mit dem angegebenen Preisdienst und ohne Wartezeit zwischen den Abfragen.
    /// </summary>
    /// <param name="prices">Der Preisdienst.</param>
    /// <returns>Der Dienst.</returns>
    public static RouteStationSearchService StationSearch(IFuelPriceService prices)
    {
        return new RouteStationSearchService(prices, NoWaitPacer());
    }
}
