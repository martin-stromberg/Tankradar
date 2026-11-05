using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft Client, Dienst und Preis-Cache gegen einen lokalen Mock-Server über echtes HTTP (kein produktiver Endpunkt): Abruf, Wiederholung, Zeitlimit, Offline-Rückfall.
/// </summary>
public class PriceServiceMockServerTests_Flow : IDisposable
{
    private readonly MockTankerkoenigServer _server = new();
    private readonly TestDatabase _database = new();
    private readonly SwitchableConnection _connection = new();
    private readonly SwitchableTime _clock = new();

    private FuelPriceService CreateService(string? key = MockTankerkoenigServer.AcceptedKey, TimeSpan? timeout = null, Uri? baseUrl = null)
    {
        var options = new PriceApiOptions
        {
            BaseUrl = baseUrl ?? _server.BaseUrl,
            AllowLoopbackHttp = true,
            RequestTimeout = timeout ?? TimeSpan.FromSeconds(3),
            RetryBaseDelay = TimeSpan.FromMilliseconds(20),
            MinRequestInterval = TimeSpan.Zero,
        };
        var delay = new TaskDelay();
        var client = new TankerkoenigClient(
            new HttpClient { Timeout = Timeout.InfiniteTimeSpan },
            new FixedKey(key),
            options,
            delay,
            new RequestThrottle(options.MinRequestInterval, delay, _clock),
            _clock,
            NullLogger<TankerkoenigClient>.Instance);
        var factory = _database.CreateFactory();
        var repository = new PriceRepository(factory, _database.CreateInitializer(factory));
        return new FuelPriceService(client, repository, _connection, options, _clock, NullLogger<FuelPriceService>.Instance);
    }

    private static StationSearchQuery Query => new(52.52, 13.405, 5, [FuelType.SuperE5, FuelType.SuperE10, FuelType.Diesel]);

    /// <summary>
    /// Prüft den vollständigen Abruf gegen den Mock: Stationen, Preise, abgeleitete Hinweise.
    /// </summary>
    [Fact]
    public async Task Search_AgainstMock_ReturnsStationsAndPrices()
    {
        var result = await CreateService().SearchNearbyAsync(Query);

        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.Equal(["Alpha Tankstelle", "Beta Tankstelle"], result.Stations.Select(s => s.Name));
        Assert.Equal(3, result.Stations[0].Prices.Count);
        Assert.Equal(2, result.Stations[1].Prices.Count);
        Assert.Equal(1, _server.ListRequests);
    }

    /// <summary>
    /// Prüft den Detailabruf gegen den Mock und die Ableitung „Automatentankstelle“ aus durchgehenden Öffnungszeiten.
    /// </summary>
    [Fact]
    public async Task Detail_AgainstMock_DerivesAutomatedStationFromOpeningTimes()
    {
        var service = CreateService();

        var alpha = (await service.GetStationDetailAsync(MockTankerkoenigServer.StationAlpha)).Station!;
        var beta = (await service.GetStationDetailAsync(MockTankerkoenigServer.StationBeta)).Station!;

        Assert.True(StationHints.IsAutomatedStation(alpha.WholeDay, alpha.OpeningTimes));
        Assert.False(StationHints.IsAutomatedStation(beta.WholeDay, beta.OpeningTimes));
        Assert.Equal("Mo-Fr", Assert.Single(beta.OpeningTimes).Text);
        Assert.Equal(2, _server.DetailRequests);
    }

    /// <summary>
    /// Prüft, dass vorübergehende Serverfehler wiederholt werden und der Abruf danach gelingt.
    /// </summary>
    [Fact]
    public async Task Search_TransientServerErrors_AreRetried()
    {
        _server.EnqueueStatuses(503, 502);

        var result = await CreateService().SearchNearbyAsync(Query);

        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.Equal(3, _server.ListRequests);
    }

    /// <summary>
    /// Prüft das Zeitlimit: Antwortet der Server zu langsam, fällt der Dienst auf die zuletzt bekannten Preise zurück.
    /// </summary>
    [Fact]
    public async Task Search_SlowServer_TimesOutAndFallsBackToCache()
    {
        await CreateService().SearchNearbyAsync(Query);
        _clock.Advance(TimeSpan.FromMinutes(30));
        _server.ResponseDelay = TimeSpan.FromSeconds(1);

        // Zweiter Dienst mit kurzem Zeitlimit auf derselben Datenbank (der erste Abruf lief mit grosszuegigem Limit).
        var result = await CreateService(timeout: TimeSpan.FromMilliseconds(150)).SearchNearbyAsync(Query);

        Assert.Equal(PriceDataSource.OfflineFallback, result.Source);
        Assert.Equal(PriceFailure.Unreachable, result.Failure);
        Assert.Equal(2, result.Stations.Count);
        Assert.Equal("vor 30 Min.", PriceFreshness.FormatAge(result.Stations[0].Prices[0].RetrievedUtc, _clock.GetUtcNow().UtcDateTime));
    }

    /// <summary>
    /// Prüft den Offline-Rückfall bei nicht erreichbarem Dienst (Server beendet) mit den zuvor gespeicherten Preisen.
    /// </summary>
    [Fact]
    public async Task Search_ServerGone_ReturnsLastKnownPrices()
    {
        var service = CreateService();
        await service.SearchNearbyAsync(Query);
        var closedUrl = _server.BaseUrl;
        _server.Dispose();
        _clock.Advance(TimeSpan.FromMinutes(10));

        var offline = CreateService(baseUrl: closedUrl);
        var result = await offline.SearchNearbyAsync(Query);

        Assert.Equal(PriceDataSource.OfflineFallback, result.Source);
        Assert.Equal(PriceFailure.Unreachable, result.Failure);
        Assert.Equal(2, result.Stations.Count);
    }

    /// <summary>
    /// Prüft, dass ein falscher Schlüssel als abgelehnt gemeldet wird und ein fehlender Schlüssel keinen Abruf auslöst.
    /// </summary>
    [Fact]
    public async Task Search_KeyProblems_AreReportedWithoutThrowing()
    {
        var wrong = await CreateService(key: "falsch").SearchNearbyAsync(Query);
        var requestsBefore = _server.TotalRequests;
        var missing = await CreateService(key: null).SearchNearbyAsync(Query);

        Assert.Equal(PriceFailure.Rejected, wrong.Failure);
        Assert.Equal(PriceFailure.ApiKeyMissing, missing.Failure);
        Assert.Equal(requestsBefore, _server.TotalRequests);
    }

    /// <summary>
    /// Prüft die Erkennung einer wiederhergestellten Verbindung im Zusammenspiel mit dem Dienst.
    /// </summary>
    [Fact]
    public async Task Search_ConnectionRestored_FetchesLiveAgain()
    {
        var service = CreateService();
        _connection.Change(false);
        var offline = await service.SearchNearbyAsync(Query);

        _connection.Change(true);
        var online = await service.SearchNearbyAsync(Query);

        Assert.Equal(PriceFailure.Offline, offline.Failure);
        Assert.Empty(offline.Stations);
        Assert.Equal(PriceDataSource.Live, online.Source);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _server.Dispose();
        _database.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class FixedKey : IApiKeyProvider
    {
        private readonly string? _value;

        public FixedKey(string? value)
        {
            _value = value;
        }

        public Task<string?> GetApiKeyAsync()
        {
            return Task.FromResult(_value);
        }
    }

    private sealed class SwitchableConnection : IConnectionMonitor
    {
        public event EventHandler<bool>? ConnectionChanged;

        public event EventHandler? ConnectionRestored;

        public bool IsOnline { get; private set; } = true;

        public void Change(bool online)
        {
            IsOnline = online;
            ConnectionChanged?.Invoke(this, online);
            if (online)
            {
                ConnectionRestored?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private sealed class SwitchableTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan by)
        {
            _now += by;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}
