using System.Globalization;
using System.Net;
using System.Text;

namespace Tankradar.TestSupport;

/// <summary>
/// Lokaler Mock der Tankerkönig-API auf einer Loopback-Adresse (ausschließlich für Tests; kein produktiver Endpunkt).
/// Beantwortet <c>list.php</c>, <c>detail.php</c> und <c>prices.php</c> mit festen Testdaten und lässt sich zu Fehlerantworten und Verzögerungen anweisen.
/// </summary>
public sealed class MockTankerkoenigServer : IDisposable
{
    /// <summary>
    /// Der Schlüssel, den der Mock akzeptiert.
    /// </summary>
    public const string AcceptedKey = "mock-key-for-tests";

    /// <summary>
    /// Kennung der ersten Teststation (rund um Berlin-Mitte, durchgehend geöffnet).
    /// </summary>
    public const string StationAlpha = "11111111-1111-4111-8111-111111111111";

    /// <summary>
    /// Kennung der zweiten Teststation (mit festen Öffnungszeiten und ohne Diesel-Preis).
    /// </summary>
    public const string StationBeta = "22222222-2222-4222-8222-222222222222";

    /// <summary>
    /// Kennung der dritten Teststation (rund 8 km von Berlin-Mitte, nur Super E5 und Diesel, Diesel am günstigsten, nicht durchgehend geöffnet).
    /// </summary>
    public const string StationGamma = "33333333-3333-4333-8333-333333333333";

    /// <summary>
    /// Kennung der vierten Teststation (rund 22 km von Berlin-Mitte, alle Sorten, durchgehend geöffnet).
    /// </summary>
    public const string StationDelta = "44444444-4444-4444-8444-444444444444";

    /// <summary>
    /// Kennung der fünften Teststation (rund 40 km von Berlin-Mitte, außerhalb jedes zulässigen Suchradius).
    /// </summary>
    public const string StationEpsilon = "55555555-5555-4555-8555-555555555555";

    /// <summary>
    /// Kennung der Teststation mit Inhabernamen (nur nach <see cref="AddOwnerNamedStation"/> vorhanden): Name „Klaus Heinrich“, Marke „Westfalen“, wenige hundert Meter von Berlin-Mitte.
    /// </summary>
    public const string StationZeta = "66666666-6666-4666-8666-666666666666";

    /// <summary>
    /// Kennung der ersten Teststation der Marke „JET“ (nur nach <see cref="AddSameBrandStations"/> vorhanden): Name „JET BERLIN HAUPTSTR. 1“.
    /// </summary>
    public const string StationJetMain = "77777777-7777-4777-8777-777777777777";

    /// <summary>
    /// Kennung der zweiten Teststation der Marke „JET“ im selben Ort (nur nach <see cref="AddSameBrandStations"/> vorhanden): Name „JET BERLIN NEBENWEG 22“.
    /// </summary>
    public const string StationJetSide = "88888888-8888-4888-8888-888888888888";

    private const double MaxRadiusKm = 25;
    private const double DefaultLatitude = 52.52;
    private const double DefaultLongitude = 13.405;

    private static readonly MockStation[] GenericStations =
    [
        new(StationGamma, "Gamma Tankstelle", "", "Gammaweg", "3", "13125", "Berlin", 52.5920, 13.4050, 1.899m, null, 1.659m, false, "Mo-Fr", "06:00:00", "22:00:00"),
        new(StationDelta, "Delta Tankstelle", "", "Deltaallee", "4", "16225", "Eberswalde", 52.5200, 13.7300, 1.829m, 1.769m, 1.689m, true, "Mo-So", "00:00:00", "24:00:00"),
        new(StationEpsilon, "Epsilon Tankstelle", "", "Epsilonstraße", "5", "16515", "Oranienburg", 52.8800, 13.4050, 1.809m, 1.749m, 1.669m, true, "Mo-So", "00:00:00", "24:00:00"),
    ];

    private readonly List<MockStation> _generated = [];
    private readonly HttpListener _listener;
    private readonly object _gate = new();
    private readonly Queue<int> _queuedStatuses = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _loop;
    private int _listRequests;
    private int _detailRequests;
    private int _pricesRequests;
    private readonly Dictionary<string, (decimal? E5, decimal? E10, decimal? Diesel)> _priceOverrides = [];
    private string? _lastPricesIds;
    private int _disposed;
    private volatile string? _lastRequest;
    private int? _lastListRadius;
    private double? _lastListLatitude;
    private double? _lastListLongitude;

    /// <summary>
    /// Startet den Server auf einem freien Port.
    /// </summary>
    public MockTankerkoenigServer()
    {
        _listener = LoopbackHttpListener.Start(out var port);
        BaseUrl = new Uri($"http://127.0.0.1:{port}/json/");
        _loop = Task.Run(RunAsync);
    }

    /// <summary>
    /// Basisadresse des Mocks (Loopback, HTTP).
    /// </summary>
    public Uri BaseUrl { get; }

    /// <summary>
    /// Wenn gesetzt, antwortet der Server verzögert (zum Prüfen des Zeitlimits).
    /// </summary>
    public TimeSpan ResponseDelay { get; set; }

    /// <summary>
    /// Anzahl der empfangenen Umkreisanfragen.
    /// </summary>
    public int ListRequests => _listRequests;

    /// <summary>
    /// Anzahl der empfangenen Detailanfragen.
    /// </summary>
    public int DetailRequests => _detailRequests;

    /// <summary>
    /// Gesamtzahl der empfangenen Anfragen.
    /// </summary>
    public int TotalRequests => ListRequests + DetailRequests + PricesRequests;

    /// <summary>
    /// Anzahl der empfangenen Preisanfragen nach Kennungen (<c>prices.php</c>).
    /// </summary>
    public int PricesRequests => _pricesRequests;

    /// <summary>
    /// Der unveränderte <c>ids</c>-Parameter der zuletzt empfangenen Preisanfrage; <see langword="null"/>, wenn keine empfangen wurde.
    /// </summary>
    public string? LastPricesIds
    {
        get
        {
            lock (_gate)
            {
                return _lastPricesIds;
            }
        }
    }

    /// <summary>
    /// Die zuletzt empfangene Anfrageadresse (Pfad und Query).
    /// </summary>
    public string? LastRequest
    {
        get => _lastRequest;
        private set => _lastRequest = value;
    }

    /// <summary>
    /// Der Radius (km) der zuletzt empfangenen Umkreisanfrage; <see langword="null"/>, wenn keine empfangen wurde oder der Wert nicht lesbar war.
    /// </summary>
    public int? LastListRadius
    {
        get
        {
            lock (_gate)
            {
                return _lastListRadius;
            }
        }
    }

    /// <summary>
    /// Der Breitengrad der zuletzt empfangenen Umkreisanfrage.
    /// </summary>
    public double? LastListLatitude
    {
        get
        {
            lock (_gate)
            {
                return _lastListLatitude;
            }
        }
    }

    /// <summary>
    /// Der Längengrad der zuletzt empfangenen Umkreisanfrage.
    /// </summary>
    public double? LastListLongitude
    {
        get
        {
            lock (_gate)
            {
                return _lastListLongitude;
            }
        }
    }

    /// <summary>
    /// Fügt dem Mock die Tankstelle <see cref="StationZeta"/> hinzu, deren Name der Inhaber ist („Klaus Heinrich“) und deren Marke „Westfalen“ lautet
    /// (wie bei Tankstellen, für die die Quelle den Inhaber als Namen liefert). Alle übrigen Teststationen liefern, wie viele echte Tankstellen, keine Marke.
    /// </summary>
    public void AddOwnerNamedStation()
    {
        lock (_gate)
        {
            if (_generated.All(station => station.Id != StationZeta))
            {
                _generated.Add(new MockStation(StationZeta, "Klaus Heinrich", "Westfalen", "Zetastraße", "6", "10117", "Berlin", 52.5230, 13.4080, 1.839m, 1.779m, 1.679m, true, "Mo-So", "00:00:00", "24:00:00"));
            }
        }
    }

    /// <summary>
    /// Fügt dem Mock die Tankstellen <see cref="StationJetMain"/> und <see cref="StationJetSide"/> hinzu: zwei Tankstellen derselben Marke „JET“ im selben Ort,
    /// deren Quellname die Straße enthält (wie bei vielen echten Tankstellen); der Anzeigename ist bei beiden „JET“.
    /// </summary>
    public void AddSameBrandStations()
    {
        lock (_gate)
        {
            if (_generated.All(station => station.Id != StationJetMain))
            {
                _generated.Add(new MockStation(StationJetMain, "JET BERLIN HAUPTSTR. 1", "JET", "Hauptstraße", "1", "10115", "Berlin", 52.5210, 13.4060, 1.849m, 1.789m, 1.689m, true, "Mo-So", "00:00:00", "24:00:00"));
                _generated.Add(new MockStation(StationJetSide, "JET BERLIN NEBENWEG 22", "JET", "Nebenweg", "22", "10115", "Berlin", 52.5215, 13.4065, 1.859m, 1.799m, 1.699m, true, "Mo-So", "00:00:00", "24:00:00"));
            }
        }
    }

    /// <summary>
    /// Fügt dem Mock zusätzliche Tankstellen hinzu (alle Sorten, alle innerhalb von etwa 3 km um Berlin-Mitte), z. B. für Tests mit großer Ergebnismenge.
    /// </summary>
    /// <param name="count">Die Anzahl der zusätzlichen Tankstellen.</param>
    public void AddGeneratedStations(int count)
    {
        lock (_gate)
        {
            var start = _generated.Count;
            for (var i = start; i < start + count; i++)
            {
                var id = new Guid(i + 1, 0, 0, [0, 0, 0, 0, 0, 0, 0, 7]).ToString("D");
                var offset = (i % 20) * 0.0012;
                var offsetLng = (i / 20 % 20) * 0.0018;
                var e5 = 1.700m + ((i * 7 % 90) / 1000m);
                _generated.Add(new MockStation(id, $"Station {i + 1:D3}", string.Empty, "Generierte Straße", (i % 50 + 1).ToString(CultureInfo.InvariantCulture), "10115", "Berlin", 52.5000 + offset, 13.3900 + offsetLng, e5, e5 - 0.060m, e5 - 0.160m, false, "Mo-Fr", "06:00:00", "22:00:00"));
            }
        }
    }

    /// <summary>
    /// Legt die Preise fest, die <c>prices.php</c> für eine Tankstelle liefert (überschreibt nur diesen Endpunkt; <see langword="null"/> ergibt <c>false</c>).
    /// </summary>
    /// <param name="stationId">Die Kennung der Tankstelle.</param>
    /// <param name="e5">Preis Super E5.</param>
    /// <param name="e10">Preis Super E10.</param>
    /// <param name="diesel">Preis Diesel.</param>
    public void SetPrices(string stationId, decimal? e5, decimal? e10, decimal? diesel)
    {
        lock (_gate)
        {
            _priceOverrides[stationId] = (e5, e10, diesel);
        }
    }

    /// <summary>
    /// Lässt die nächsten Anfragen mit den angegebenen HTTP-Statuscodes beantworten (je Anfrage einer).
    /// </summary>
    /// <param name="statuses">Die Statuscodes in Reihenfolge.</param>
    public void EnqueueStatuses(params int[] statuses)
    {
        lock (_gate)
        {
            foreach (var status in statuses)
            {
                _queuedStatuses.Enqueue(status);
            }
        }
    }

    /// <summary>
    /// Beendet den Server.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        _stop.Cancel();
        try
        {
            _listener.Close();
        }
        catch (ObjectDisposedException)
        {
            // Bereits geschlossen.
        }

        try
        {
            _loop.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Beim Schließen des Listeners erwartete Abbrüche.
        }

        _stop.Dispose();
    }

    private static string Number(double value)
    {
        return value.ToString("0.0####", CultureInfo.InvariantCulture);
    }

    private static string AlphaJson(bool detailed, double distance)
    {
        // Die Umkreissuche (list.php) liefert laut Dokumentation keine Öffnungszeiten (weder openingTimes noch wholeDay); nur detail.php tut das.
        const string AroundTheClock = ",\"wholeDay\":true,\"openingTimes\":[{\"text\":\"Mo-So\",\"start\":\"00:00:00\",\"end\":\"24:00:00\"}]";
        var extra = detailed
            ? AroundTheClock + ",\"overrides\":[],\"state\":\"open\""
            : $",\"dist\":{Number(distance)}";
        return "{\"id\":\"" + StationAlpha + "\",\"name\":\"Alpha Tankstelle\",\"brand\":\"\",\"street\":\"Hauptstraße\",\"houseNumber\":\"1\",\"postCode\":\"10115\",\"place\":\"Berlin\",\"lat\":52.5201,\"lng\":13.4051,\"isOpen\":true,\"e5\":1.859,\"e10\":1.799,\"diesel\":1.699" + extra + "}";
    }

    private static string BetaJson(bool detailed, double distance)
    {
        var extra = detailed
            ? ",\"wholeDay\":false,\"openingTimes\":[{\"text\":\"Mo-Fr\",\"start\":\"06:00:00\",\"end\":\"22:00:00\"}],\"overrides\":[]"
            : $",\"dist\":{Number(distance)}";
        return "{\"id\":\"" + StationBeta + "\",\"name\":\"Beta Tankstelle\",\"brand\":\"\",\"street\":\"Nebenweg\",\"houseNumber\":\"22\",\"postCode\":\"10117\",\"place\":\"Berlin\",\"lat\":52.5301,\"lng\":13.4151,\"isOpen\":true,\"e5\":1.879,\"e10\":1.819,\"diesel\":false" + extra + "}";
    }

    private static double DistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double EarthRadiusKm = 6371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2))
            + (Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2));
        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static string PriceOrFalse(decimal? price)
    {
        return price is { } value ? value.ToString("0.000", CultureInfo.InvariantCulture) : "false";
    }

    private static string GenericJson(MockStation station, bool detailed, double distance)
    {
        var times = ",\"wholeDay\":" + (station.WholeDay ? "true" : "false")
            + ",\"openingTimes\":[{\"text\":\"" + station.TimesText + "\",\"start\":\"" + station.TimesStart + "\",\"end\":\"" + station.TimesEnd + "\"}]";
        var extra = detailed
            ? times + ",\"overrides\":[]"
            : $",\"dist\":{Number(distance)}";
        return "{\"id\":\"" + station.Id + "\",\"name\":\"" + station.Name + "\",\"brand\":\"" + station.Brand + "\",\"street\":\"" + station.Street
            + "\",\"houseNumber\":\"" + station.HouseNumber + "\",\"postCode\":\"" + station.PostCode + "\",\"place\":\"" + station.Place
            + "\",\"lat\":" + Number(station.Latitude) + ",\"lng\":" + Number(station.Longitude)
            + ",\"isOpen\":true,\"e5\":" + PriceOrFalse(station.E5) + ",\"e10\":" + PriceOrFalse(station.E10) + ",\"diesel\":" + PriceOrFalse(station.Diesel) + extra + "}";
    }

    private static double? ParseQuery(string? value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private MockStation[] GeneratedSnapshot()
    {
        lock (_gate)
        {
            return _generated.ToArray();
        }
    }

    private string ListBody(HttpListenerContext context)
    {
        var latitude = ParseQuery(context.Request.QueryString["lat"]);
        var longitude = ParseQuery(context.Request.QueryString["lng"]);
        var radius = ParseQuery(context.Request.QueryString["rad"]);
        lock (_gate)
        {
            _lastListLatitude = latitude;
            _lastListLongitude = longitude;
            _lastListRadius = radius is { } r ? (int)Math.Round(r) : null;
        }

        if (radius is null or < 0 or > MaxRadiusKm)
        {
            return "{\"ok\":false,\"message\":\"rad ungültig oder größer als 25\"}";
        }

        var centerLat = latitude ?? DefaultLatitude;
        var centerLng = longitude ?? DefaultLongitude;
        var alphaDistance = DistanceKm(centerLat, centerLng, 52.5201, 13.4051);
        var betaDistance = DistanceKm(centerLat, centerLng, 52.5301, 13.4151);
        var entries = new List<(double Distance, string Json)>
        {
            (alphaDistance, AlphaJson(false, Math.Round(alphaDistance, 1))),
            (betaDistance, BetaJson(false, Math.Round(betaDistance, 1))),
        };
        foreach (var station in GenericStations.Concat(GeneratedSnapshot()))
        {
            var distance = DistanceKm(centerLat, centerLng, station.Latitude, station.Longitude);
            entries.Add((distance, GenericJson(station, false, Math.Round(distance, 1))));
        }

        var inRadius = entries.Where(entry => entry.Distance <= radius).OrderBy(entry => entry.Distance).Select(entry => entry.Json);
        return "{\"ok\":true,\"license\":\"CC BY 4.0 - https://creativecommons.tankerkoenig.de\",\"data\":\"MTS-K\",\"status\":\"ok\",\"stations\":[" + string.Join(",", inRadius) + "]}";
    }

    private (decimal? E5, decimal? E10, decimal? Diesel)? KnownPrices(string id)
    {
        lock (_gate)
        {
            if (_priceOverrides.TryGetValue(id, out var overridden))
            {
                return overridden;
            }
        }

        return id switch
        {
            StationAlpha => (1.859m, 1.799m, 1.699m),
            StationBeta => (1.879m, 1.819m, null),
            _ => GenericStations.Concat(GeneratedSnapshot()).FirstOrDefault(station => station.Id == id) is { } generic
                ? (generic.E5, generic.E10, generic.Diesel)
                : null,
        };
    }

    private string PricesBody(HttpListenerContext context)
    {
        var raw = context.Request.QueryString["ids"];
        lock (_gate)
        {
            _lastPricesIds = raw;
        }

        var ids = (raw ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (ids.Length == 0)
        {
            return "{\"ok\":false,\"message\":\"ids fehlt\"}";
        }

        if (ids.Length > 10)
        {
            return "{\"ok\":false,\"message\":\"maximal 10 ids erlaubt\"}";
        }

        var entries = ids.Select(id => KnownPrices(id) is { } known
            ? "\"" + id + "\":{\"status\":\"open\",\"e5\":" + PriceOrFalse(known.E5) + ",\"e10\":" + PriceOrFalse(known.E10) + ",\"diesel\":" + PriceOrFalse(known.Diesel) + "}"
            : "\"" + id + "\":{\"status\":\"no prices\"}");
        return "{\"ok\":true,\"license\":\"CC BY 4.0 - https://creativecommons.tankerkoenig.de\",\"data\":\"MTS-K\",\"prices\":{" + string.Join(",", entries) + "}}";
    }

    private async Task RunAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await HandleAsync(context).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    context.Response.Abort();
                }
            });
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        var path = context.Request.Url?.AbsolutePath ?? string.Empty;
        LastRequest = context.Request.Url?.PathAndQuery;
        var isList = path.EndsWith("/list.php", StringComparison.Ordinal);
        var isDetail = path.EndsWith("/detail.php", StringComparison.Ordinal);
        var isPrices = path.EndsWith("/prices.php", StringComparison.Ordinal);
        if (isList)
        {
            Interlocked.Increment(ref _listRequests);
        }
        else if (isPrices)
        {
            Interlocked.Increment(ref _pricesRequests);
        }
        else if (isDetail)
        {
            Interlocked.Increment(ref _detailRequests);
        }

        if (ResponseDelay > TimeSpan.Zero)
        {
            await Task.Delay(ResponseDelay).ConfigureAwait(false);
        }

        int? forcedStatus = null;
        lock (_gate)
        {
            if (_queuedStatuses.Count > 0)
            {
                forcedStatus = _queuedStatuses.Dequeue();
            }
        }

        string body;
        var status = 200;
        if (forcedStatus is { } forced)
        {
            status = forced;
            body = "{\"ok\":false,\"message\":\"erzwungener Fehler\"}";
        }
        else if (context.Request.QueryString["apikey"] != AcceptedKey)
        {
            body = "{\"ok\":false,\"message\":\"apikey nicht angegeben, falsch, oder zu lang\"}";
        }
        else if (isList)
        {
            body = ListBody(context);
        }
        else if (isPrices)
        {
            body = PricesBody(context);
        }
        else if (isDetail)
        {
            var id = context.Request.QueryString["id"];
            if (id == StationAlpha)
            {
                body = "{\"ok\":true,\"status\":\"ok\",\"station\":" + AlphaJson(true, 0) + "}";
            }
            else if (id == StationBeta)
            {
                body = "{\"ok\":true,\"status\":\"ok\",\"station\":" + BetaJson(true, 0) + "}";
            }
            else if (GenericStations.Concat(GeneratedSnapshot()).FirstOrDefault(station => station.Id == id) is { } generic)
            {
                body = "{\"ok\":true,\"status\":\"ok\",\"station\":" + GenericJson(generic, true, 0) + "}";
            }
            else
            {
                body = "{\"ok\":false,\"status\":\"error\",\"message\":\"station nicht gefunden\"}";
            }
        }
        else
        {
            status = 404;
            body = "{}";
        }

        var bytes = Encoding.UTF8.GetBytes(body);
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
        context.Response.Close();
    }
}

internal sealed record MockStation(
    string Id,
    string Name,
    string Brand,
    string Street,
    string HouseNumber,
    string PostCode,
    string Place,
    double Latitude,
    double Longitude,
    decimal? E5,
    decimal? E10,
    decimal? Diesel,
    bool WholeDay,
    string TimesText,
    string TimesStart,
    string TimesEnd);
