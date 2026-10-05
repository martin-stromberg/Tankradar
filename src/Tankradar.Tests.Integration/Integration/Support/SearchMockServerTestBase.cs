using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Location;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.ViewModels;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration.Support;

/// <summary>
/// Gemeinsamer Aufbau der Suchtests gegen den lokalen Mock-Server: echter Client, Dienst, Preis-Cache auf SQLite, Standortdienst mit festem Standort.
/// Es werden nie produktive Endpunkte angesprochen.
/// </summary>
public abstract class SearchMockServerTestBase : IDisposable
{
    /// <summary>
    /// Die Suchposition der Tests (Breitengrad); bewusst von allen Tankstellenkoordinaten verschieden.
    /// </summary>
    protected const double SearchLatitude = 52.5123456;

    /// <summary>
    /// Die Suchposition der Tests (Längengrad); bewusst von allen Tankstellenkoordinaten verschieden.
    /// </summary>
    protected const double SearchLongitude = 13.4123456;

    private readonly List<string> _logSink = [];

    /// <summary>
    /// Der Mock-Server.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected MockTankerkoenigServer Server { get; private set; } = new();

    /// <summary>
    /// Die Testdatenbank.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected TestDatabase Database { get; } = new();

    /// <summary>
    /// Die Verbindung.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected SwitchableConnection Connection { get; } = new();

    /// <summary>
    /// Die Uhr.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected SwitchableTime Clock { get; } = new();

    /// <summary>
    /// Alle protokollierten Texte der beteiligten Dienste.
    /// </summary>
    protected string LogText
    {
        get
        {
            lock (_logSink)
            {
                return string.Join('\n', _logSink);
            }
        }
    }

    /// <summary>
    /// Alle drei Sorten als Anfrage.
    /// </summary>
    protected static IReadOnlyList<FuelType> AllFuels { get; } = [FuelType.SuperE5, FuelType.SuperE10, FuelType.Diesel];

    /// <summary>
    /// Erstellt die Anfrage an der Suchposition.
    /// </summary>
    /// <param name="radiusKm">Der Radius.</param>
    /// <returns>Die Anfrage.</returns>
    protected static StationSearchQuery Query(int radiusKm)
    {
        return new StationSearchQuery(SearchLatitude, SearchLongitude, radiusKm, AllFuels);
    }

    /// <summary>
    /// Erstellt den Preisdienst gegen den Mock-Server (oder eine andere Adresse).
    /// </summary>
    /// <param name="baseUrl">Die Adresse oder <see langword="null"/> für den Mock-Server.</param>
    /// <param name="key">Der Schlüssel.</param>
    /// <returns>Der Dienst.</returns>
    protected FuelPriceService CreateService(Uri? baseUrl = null, string? key = MockTankerkoenigServer.AcceptedKey)
    {
        var options = new PriceApiOptions
        {
            BaseUrl = baseUrl ?? Server.BaseUrl,
            AllowLoopbackHttp = true,
            RequestTimeout = TimeSpan.FromSeconds(3),
            RetryBaseDelay = TimeSpan.FromMilliseconds(20),
            MinRequestInterval = TimeSpan.Zero,
        };
        var delay = new TaskDelay();
        var client = new TankerkoenigClient(
            new HttpClient { Timeout = Timeout.InfiniteTimeSpan },
            new FixedKey(key),
            options,
            delay,
            new RequestThrottle(options.MinRequestInterval, delay, Clock),
            Clock,
            new RecordingLogger<TankerkoenigClient>(_logSink));
        var factory = Database.CreateFactory();
        var repository = new PriceRepository(factory, Database.CreateInitializer(factory));
        return new FuelPriceService(client, repository, Connection, options, Clock, new RecordingLogger<FuelPriceService>(_logSink));
    }

    /// <summary>
    /// Erstellt das ViewModel der Suche mit dem übergebenen Preisdienst und festem Teststandort.
    /// </summary>
    /// <param name="service">Der Preisdienst.</param>
    /// <returns>Das ViewModel.</returns>
    protected MapViewModel CreateViewModel(FuelPriceService service)
    {
        var factory = Database.CreateFactory();
        var settings = new SettingsService(factory, Database.CreateInitializer(factory));
        return new MapViewModel(
            settings,
            new TestLocationService(new GeoPosition(SearchLatitude, SearchLongitude)),
            service,
            Connection,
            Clock,
            new RecordingLogger<MapViewModel>(_logSink));
    }

    /// <summary>
    /// Beendet den Mock-Server und gibt seine Adresse zurück.
    /// </summary>
    /// <returns>Die Adresse des beendeten Servers.</returns>
    protected Uri StopServer()
    {
        var url = Server.BaseUrl;
        Server.Dispose();
        return url;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Server.Dispose();
        Database.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Schlüsselanbieter mit festem Wert.
    /// </summary>
    protected sealed class FixedKey : IApiKeyProvider
    {
        private readonly string? _value;

        /// <summary>
        /// Erstellt den Anbieter.
        /// </summary>
        /// <param name="value">Der Schlüssel oder <see langword="null"/>.</param>
        public FixedKey(string? value)
        {
            _value = value;
        }

        /// <inheritdoc />
        public Task<string?> GetApiKeyAsync()
        {
            return Task.FromResult(_value);
        }
    }

    /// <summary>
    /// Umschaltbare Verbindung.
    /// </summary>
    protected sealed class SwitchableConnection : IConnectionMonitor
    {
        /// <inheritdoc />
        public event EventHandler<bool>? ConnectionChanged;

        /// <inheritdoc />
        public event EventHandler? ConnectionRestored;

        /// <inheritdoc />
        public bool IsOnline { get; private set; } = true;

        /// <summary>
        /// Setzt den Zustand und meldet die Änderung.
        /// </summary>
        /// <param name="online">Der neue Zustand.</param>
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

    /// <summary>
    /// Steuerbare Uhr.
    /// </summary>
    protected sealed class SwitchableTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// Stellt die Uhr vor.
        /// </summary>
        /// <param name="by">Die Dauer.</param>
        public void Advance(TimeSpan by)
        {
            _now += by;
        }

        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}
