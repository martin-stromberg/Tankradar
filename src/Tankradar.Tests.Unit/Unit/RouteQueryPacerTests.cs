using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Mindestabstand zwischen den Preisabfragen der Routensuche (Taktgeber und Ablauf der Stationssuche), auch über mehrere Suchen hinweg.
/// </summary>
public class RouteQueryPacerTests : BaseTest
{
    private static readonly FuelType[] Fuels = [FuelType.Diesel];

    private readonly ManualTimeProvider _clock = new();
    private readonly AdvancingDelay _delay;
    private readonly ScriptedFuelPriceService _prices = new();
    private readonly RouteProfile _route = RouteTestData.NorthFromBerlin(130 / 111.195);

    /// <summary>
    /// Erstellt den Aufbau mit dem vorläufigen Mindestabstand von 10 s und einer Uhr, die beim Warten vorrückt.
    /// </summary>
    public RouteQueryPacerTests()
    {
        _delay = new AdvancingDelay(_clock);
        _prices.Respond = (_, _) => RouteStationFactory.Live();
    }

    private RouteQueryPacer CreatePacer()
    {
        return new RouteQueryPacer(new RouteSearchOptions(), _delay, _clock);
    }

    private IReadOnlyList<RouteQueryPoint> Queries()
    {
        Assert.True(RouteSampler.TryCreateQueryPoints(_route, 2, new RouteSearchOptions(), out var queries));
        Assert.Equal(3, queries.Count);
        return queries;
    }

    /// <summary>
    /// Prüft, dass ohne vorherige Abfrage nicht gewartet wird und danach der Rest bis zum Mindestabstand abgewartet wird.
    /// </summary>
    [Fact]
    public async Task WaitAsync_WaitsOnlyTheRemainingInterval()
    {
        var pacer = CreatePacer();

        await pacer.WaitAsync(CancellationToken.None);
        Assert.Empty(_delay.Delays);

        pacer.MarkQueried();
        _clock.Advance(TimeSpan.FromSeconds(4));
        await pacer.WaitAsync(CancellationToken.None);

        Assert.Equal([TimeSpan.FromSeconds(6)], _delay.Delays);

        _delay.Delays.Clear();
        _clock.Advance(TimeSpan.FromSeconds(10));
        await pacer.WaitAsync(CancellationToken.None);
        Assert.Empty(_delay.Delays);
    }

    /// <summary>
    /// Prüft, dass ein Abbruch die Wartezeit beendet.
    /// </summary>
    [Fact]
    public async Task WaitAsync_Cancellation_EndsTheWait()
    {
        var pacer = new RouteQueryPacer(new RouteSearchOptions(), new TaskDelay(), TimeProvider.System);
        pacer.MarkQueried();
        using var cancellation = new CancellationTokenSource();
        cancellation.CancelAfter(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pacer.WaitAsync(cancellation.Token));
    }

    /// <summary>
    /// Prüft, dass zwischen den drei Abfragen einer Routensuche je 10 s liegen und vor der ersten nicht gewartet wird.
    /// </summary>
    [Fact]
    public async Task SearchAsync_LiveQueries_AreTenSecondsApart()
    {
        var service = new RouteStationSearchService(_prices, CreatePacer());

        await service.SearchAsync(_route, Queries(), 2, Fuels, null, CancellationToken.None);

        Assert.Equal(3, _prices.Queries.Count);
        Assert.Equal([TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)], _delay.Delays);
    }

    /// <summary>
    /// Prüft, dass der Fortschritt „k von n“ schon vor der Wartezeit gemeldet wird, damit die Anzeige während des Wartens die anstehende Abfrage nennt.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ReportsProgressBeforeWaiting()
    {
        var progress = new ListProgress();
        var seenAtWait = new List<int>();
        _delay.OnDelay = () => seenAtWait.Add(progress.Reports.Count);
        var service = new RouteStationSearchService(_prices, CreatePacer());

        await service.SearchAsync(_route, Queries(), 2, Fuels, progress, CancellationToken.None);

        Assert.Equal([2, 3], seenAtWait);
        Assert.Equal([1, 2, 3], progress.Reports.Select(report => report.Current));
    }

    /// <summary>
    /// Prüft, dass aus dem Cache bediente Abfragen keine Wartezeit auslösen.
    /// </summary>
    [Fact]
    public async Task SearchAsync_CachedQueries_DoNotWait()
    {
        _prices.Respond = (_, _) => new StationSearchResult([], PriceDataSource.Cache, PriceFailure.None);
        var service = new RouteStationSearchService(_prices, CreatePacer());

        await service.SearchAsync(_route, Queries(), 2, Fuels, null, CancellationToken.None);

        Assert.Equal(3, _prices.Queries.Count);
        Assert.Empty(_delay.Delays);
    }

    /// <summary>
    /// Prüft, dass der Abstand auch zwischen zwei Suchen gilt: Die erste Abfrage der nächsten Suche wartet den Rest.
    /// </summary>
    [Fact]
    public async Task SearchAsync_SecondSearchRightAfterFirst_WaitsBeforeItsFirstQuery()
    {
        var service = new RouteStationSearchService(_prices, CreatePacer());
        await service.SearchAsync(_route, Queries(), 2, Fuels, null, CancellationToken.None);
        _delay.Delays.Clear();
        _clock.Advance(TimeSpan.FromSeconds(3));

        await service.SearchAsync(_route, Queries(), 2, Fuels, null, CancellationToken.None);

        Assert.Equal([TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)], _delay.Delays);
    }

    /// <summary>
    /// Prüft, dass ein Abbruch während der Wartezeit die Suche beendet, ohne dass weitere Abfragen folgen.
    /// </summary>
    [Fact]
    public async Task SearchAsync_CancelledDuringWait_StopsWithoutFurtherQueries()
    {
        using var cancellation = new CancellationTokenSource();
        _delay.OnDelay = cancellation.Cancel;
        var service = new RouteStationSearchService(_prices, CreatePacer());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.SearchAsync(_route, Queries(), 2, Fuels, null, cancellation.Token));

        Assert.Single(_prices.Queries);
    }

    /// <summary>
    /// Prüft, dass ein Fehler der Preisabfrage (der den Dienst erreicht hat) ebenfalls den Takt vermerkt, ein Offline-Fehler dagegen nicht.
    /// </summary>
    [Fact]
    public async Task SearchAsync_FailureMarksQueryUnlessOffline()
    {
        var pacer = CreatePacer();
        var service = new RouteStationSearchService(_prices, pacer);
        _prices.Respond = (_, _) => new StationSearchResult([], PriceDataSource.OfflineFallback, PriceFailure.Offline);
        await service.SearchAsync(_route, Queries(), 2, Fuels, null, CancellationToken.None);
        await pacer.WaitAsync(CancellationToken.None);
        Assert.Empty(_delay.Delays);

        _prices.Respond = (_, _) => new StationSearchResult([], PriceDataSource.OfflineFallback, PriceFailure.Unreachable);
        await service.SearchAsync(_route, Queries(), 2, Fuels, null, CancellationToken.None);
        await pacer.WaitAsync(CancellationToken.None);

        Assert.Equal([TimeSpan.FromSeconds(10)], _delay.Delays);
    }

    private sealed class AdvancingDelay : IDelay
    {
        private readonly ManualTimeProvider _clock;

        public AdvancingDelay(ManualTimeProvider clock)
        {
            _clock = clock;
        }

        public List<TimeSpan> Delays { get; } = [];

        public Action? OnDelay { get; set; }

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delays.Add(delay);
            OnDelay?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            _clock.Advance(delay);
            return Task.CompletedTask;
        }
    }
}
