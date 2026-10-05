using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Tankradar.TestSupport;

/// <summary>
/// Lokaler Mock der Tankerkönig-API auf einer Loopback-Adresse (ausschließlich für Tests; kein produktiver Endpunkt).
/// Beantwortet <c>list.php</c> und <c>detail.php</c> mit festen Testdaten und lässt sich zu Fehlerantworten und Verzögerungen anweisen.
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

    private readonly HttpListener _listener = new();
    private readonly object _gate = new();
    private readonly Queue<int> _queuedStatuses = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _loop;
    private int _listRequests;
    private int _detailRequests;
    private int _disposed;
    private volatile string? _lastRequest;

    /// <summary>
    /// Startet den Server auf einem freien Port.
    /// </summary>
    public MockTankerkoenigServer()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        BaseUrl = new Uri($"http://127.0.0.1:{port}/json/");
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Start();
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
    public int TotalRequests => ListRequests + DetailRequests;

    /// <summary>
    /// Die zuletzt empfangene Anfrageadresse (Pfad und Query).
    /// </summary>
    public string? LastRequest
    {
        get => _lastRequest;
        private set => _lastRequest = value;
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
        var extra = detailed
            ? ",\"wholeDay\":true,\"openingTimes\":[{\"text\":\"Mo-So\",\"start\":\"00:00:00\",\"end\":\"24:00:00\"}],\"overrides\":[],\"state\":\"open\""
            : $",\"dist\":{Number(distance)}";
        return "{\"id\":\"" + StationAlpha + "\",\"name\":\"Alpha Tankstelle\",\"brand\":\"ALPHA\",\"street\":\"Hauptstraße\",\"houseNumber\":\"1\",\"postCode\":\"10115\",\"place\":\"Berlin\",\"lat\":52.5201,\"lng\":13.4051,\"isOpen\":true,\"e5\":1.859,\"e10\":1.799,\"diesel\":1.699" + extra + "}";
    }

    private static string BetaJson(bool detailed, double distance)
    {
        var extra = detailed
            ? ",\"wholeDay\":false,\"openingTimes\":[{\"text\":\"Mo-Fr\",\"start\":\"06:00:00\",\"end\":\"22:00:00\"}],\"overrides\":[]"
            : $",\"dist\":{Number(distance)}";
        return "{\"id\":\"" + StationBeta + "\",\"name\":\"Beta Tankstelle\",\"brand\":\"BETA\",\"street\":\"Nebenweg\",\"houseNumber\":\"22\",\"postCode\":\"10117\",\"place\":\"Berlin\",\"lat\":52.5301,\"lng\":13.4151,\"isOpen\":true,\"e5\":1.879,\"e10\":1.819,\"diesel\":false" + extra + "}";
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
        if (isList)
        {
            Interlocked.Increment(ref _listRequests);
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
            body = "{\"ok\":true,\"license\":\"CC BY 4.0 - https://creativecommons.tankerkoenig.de\",\"data\":\"MTS-K\",\"status\":\"ok\",\"stations\":["
                + AlphaJson(false, 0.1) + "," + BetaJson(false, 1.4) + "]}";
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
