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
        return new RouteQueryPacer(new RouteSearchOptions { QueryIntervalReserve = TimeSpan.Zero }, _delay, _clock);
    }

    private IReadOnlyList<RouteQueryPoint> Queries()
    {
        Assert.True(RouteSampler.TryCreateQueryPoints(_route, 2, new RouteSearchOptions(), out var queries));
        Assert.Equal(3, queries.Count);
        return queries;
    }

    /// <summary>
    /// Prüft die Reservierung: Die erste Abfrage wartet nicht, die nächste ein Intervall nach der ersten, eine spätere nur den Rest.
    /// </summary>
    [Fact]
    public async Task WaitAsync_ReservesSlotsOneIntervalApart()
    {
        var pacer = CreatePacer();

        await pacer.WaitAsync(CancellationToken.None);
        Assert.Empty(_delay.Delays);

        _clock.Advance(TimeSpan.FromSeconds(4));
        await pacer.WaitAsync(CancellationToken.None);
        Assert.Equal([TimeSpan.FromSeconds(6)], _delay.Delays);

        _delay.Delays.Clear();
        _clock.Advance(TimeSpan.FromSeconds(10));
        await pacer.WaitAsync(CancellationToken.None);
        Assert.Empty(_delay.Delays);
    }

    /// <summary>
    /// Prüft, dass auf den Mindestabstand die Reserve aufgeschlagen wird (Standard 250 ms).
    /// </summary>
    [Fact]
    public async Task WaitAsync_UsesIntervalPlusReserve()
    {
        var pacer = new RouteQueryPacer(new RouteSearchOptions(), _delay, _clock);

        await pacer.WaitAsync(CancellationToken.None);
        await pacer.WaitAsync(CancellationToken.None);
        await pacer.WaitAsync(CancellationToken.None);

        Assert.Equal(TimeSpan.FromMilliseconds(250), RouteSearchOptions.DefaultQueryIntervalReserve);
        Assert.Equal([TimeSpan.FromSeconds(10.25), TimeSpan.FromSeconds(10.25)], _delay.Delays);
    }

    /// <summary>
    /// Prüft, dass ohne Mindestabstand auch keine Reserve gilt und ein Abbruch in der Wartezeit die Reserve mit freigibt.
    /// </summary>
    [Fact]
    public async Task WaitAsync_ReserveOnlyAppliesWithInterval_AndIsReleasedOnCancel()
    {
        var withoutInterval = new RouteSearchOptions { MinQueryInterval = TimeSpan.Zero };
        Assert.Equal(TimeSpan.Zero, withoutInterval.EffectiveQueryInterval);
        Assert.Equal(TimeSpan.FromSeconds(10.25), new RouteSearchOptions().EffectiveQueryInterval);

        var pacer = new RouteQueryPacer(new RouteSearchOptions(), _delay, _clock);
        await pacer.WaitAsync(CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        _delay.OnDelay = cancellation.Cancel;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pacer.WaitAsync(cancellation.Token));
        _delay.OnDelay = null;
        _delay.Delays.Clear();

        await pacer.WaitAsync(CancellationToken.None);

        Assert.Equal([TimeSpan.FromSeconds(10.25)], _delay.Delays);
    }

    /// <summary>
    /// Prüft die Nebenläufigkeit: Gleichzeitige Aufrufer erhalten aufeinanderfolgende Zeitpunkte im Abstand des Intervalls und passieren nicht beide ungebremst.
    /// </summary>
    [Fact]
    public async Task WaitAsync_ConcurrentCallers_GetConsecutiveSlots()
    {
        var pacer = CreatePacer();
        var holdDelays = new TaskCompletionSource();
        _delay.Hold = holdDelays.Task;

        await pacer.WaitAsync(CancellationToken.None);
        var second = pacer.WaitAsync(CancellationToken.None);
        var third = pacer.WaitAsync(CancellationToken.None);
        holdDelays.SetResult();
        await Task.WhenAll(second, third);

        Assert.Equal([TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20)], _delay.Delays.Order());
    }

    /// <summary>
    /// Prüft, dass ein Abbruch in der Wartezeit die Reservierung wieder freigibt (es wurde nichts gesendet).
    /// </summary>
    [Fact]
    public async Task WaitAsync_CancelledWhileWaiting_ReleasesSlot()
    {
        var pacer = CreatePacer();
        await pacer.WaitAsync(CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        _delay.OnDelay = cancellation.Cancel;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pacer.WaitAsync(cancellation.Token));
        _delay.OnDelay = null;
        _delay.Delays.Clear();
        await pacer.WaitAsync(CancellationToken.None);

        Assert.Equal([TimeSpan.FromSeconds(10)], _delay.Delays);
    }

    /// <summary>
    /// Prüft, dass ein Abbruch die Wartezeit beendet.
    /// </summary>
    [Fact]
    public async Task WaitAsync_Cancellation_EndsTheWait()
    {
        var pacer = new RouteQueryPacer(new RouteSearchOptions(), new TaskDelay(), TimeProvider.System);
        await pacer.WaitAsync(CancellationToken.None);
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
        _prices.Cached = _ => true;
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
    /// Prüft, dass eine Cache-Antwort nach einer Live-Suche nicht wartet, auch wenn die letzte Live-Abfrage erst kurz zurückliegt.
    /// </summary>
    [Fact]
    public async Task SearchAsync_LiveThenCachedSearch_DoesNotWait()
    {
        var service = new RouteStationSearchService(_prices, CreatePacer());
        await service.SearchAsync(_route, Queries(), 2, Fuels, null, CancellationToken.None);
        _delay.Delays.Clear();
        _clock.Advance(TimeSpan.FromSeconds(3));
        _prices.Cached = _ => true;

        await service.SearchAsync(_route, Queries(), 2, Fuels, null, CancellationToken.None);

        Assert.Empty(_delay.Delays);
        Assert.Equal(6, _prices.Queries.Count);
    }

    /// <summary>
    /// Prüft, dass eine während der Anfrage abgebrochene Abfrage ihren Zeitpunkt behält: Die nächste Suche wartet trotzdem.
    /// </summary>
    [Fact]
    public async Task SearchAsync_CancelledWhileQueryIsInFlight_NextSearchStillWaits()
    {
        using var cancellation = new CancellationTokenSource();
        _prices.Respond = (_, _) =>
        {
            cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
            return RouteStationFactory.Live();
        };
        var service = new RouteStationSearchService(_prices, CreatePacer());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SearchAsync(_route, Queries(), 2, Fuels, null, cancellation.Token));
        _prices.Respond = (_, _) => RouteStationFactory.Live();
        _clock.Advance(TimeSpan.FromSeconds(2));

        await service.SearchAsync(_route, Queries(), 2, Fuels, null, CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(8), _delay.Delays[0]);
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

        public Task? Hold { get; set; }

        public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delays.Add(delay);
            OnDelay?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            if (Hold is { } hold)
            {
                await hold;
            }

            _clock.Advance(delay);
        }
    }
}
