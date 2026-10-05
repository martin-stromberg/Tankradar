using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Tankradar.TestSupport;

/// <summary>
/// Lokaler Mock von OpenStreetMap-Nominatim auf einer Loopback-Adresse (ausschließlich für Tests; kein produktiver Endpunkt).
/// Beantwortet <c>search</c> mit festen Testorten, verlangt wie das Original eine identifizierende Kennung (<c>User-Agent</c>),
/// protokolliert Anfragen samt Zeitpunkt (zum Prüfen der Anfragebegrenzung) und lässt sich zu Fehlerantworten und Verzögerungen anweisen.
/// </summary>
public sealed class MockNominatimServer : IDisposable
{
    /// <summary>
    /// Suchbegriff, der zu Berlin-Mitte (52,5200 / 13,4050) führt; auch „10115“ wird dorthin aufgelöst.
    /// </summary>
    public const string QueryBerlin = "Berlin";

    /// <summary>
    /// Suchbegriff, der zu einem Ort bei der Teststation „Epsilon“ (52,8800 / 13,4050) führt.
    /// </summary>
    public const string QueryOranienburg = "Oranienburg";

    /// <summary>
    /// Suchbegriff, der zu einem Ort bei der Teststation „Delta“ (52,5200 / 13,7300) führt.
    /// </summary>
    public const string QueryEberswalde = "Eberswalde";

    /// <summary>
    /// Suchbegriff, zu dem der Mock keinen Ort kennt (leere Trefferliste).
    /// </summary>
    public const string QueryUnknown = "Nirgendwoburg";

    private static readonly (string Key, string Latitude, string Longitude, string DisplayName)[] Places =
    [
        ("10115", "52.5200", "13.4050", "10115, Mitte, Berlin, Deutschland"),
        ("berlin", "52.5200", "13.4050", "Berlin, Deutschland"),
        ("oranienburg", "52.8800", "13.4050", "Oranienburg, Landkreis Oberhavel, Brandenburg, Deutschland"),
        ("eberswalde", "52.5200", "13.7300", "Eberswalde, Landkreis Barnim, Brandenburg, Deutschland"),
    ];

    private readonly HttpListener _listener = new();
    private readonly object _gate = new();
    private readonly Queue<int> _queuedStatuses = new();
    private readonly Queue<string> _queuedBodies = new();
    private readonly List<DateTimeOffset> _timestamps = [];
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _loop;
    private int _disposed;
    private string? _lastQuery;
    private string? _lastUserAgent;
    private string? _lastRawRequest;

    /// <summary>
    /// Startet den Server auf einem freien Port.
    /// </summary>
    public MockNominatimServer()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        BaseUrl = new Uri($"http://127.0.0.1:{port}/");
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
    /// Anzahl der empfangenen Suchanfragen.
    /// </summary>
    public int Requests
    {
        get
        {
            lock (_gate)
            {
                return _timestamps.Count;
            }
        }
    }

    /// <summary>
    /// Die Empfangszeitpunkte aller Suchanfragen in Reihenfolge.
    /// </summary>
    public IReadOnlyList<DateTimeOffset> RequestTimes
    {
        get
        {
            lock (_gate)
            {
                return _timestamps.ToArray();
            }
        }
    }

    /// <summary>
    /// Der Suchbegriff (Parameter <c>q</c>) der zuletzt empfangenen Anfrage.
    /// </summary>
    public string? LastQuery
    {
        get
        {
            lock (_gate)
            {
                return _lastQuery;
            }
        }
    }

    /// <summary>
    /// Der <c>User-Agent</c> der zuletzt empfangenen Anfrage.
    /// </summary>
    public string? LastUserAgent
    {
        get
        {
            lock (_gate)
            {
                return _lastUserAgent;
            }
        }
    }

    /// <summary>
    /// Pfad und Query der zuletzt empfangenen Anfrage.
    /// </summary>
    public string? LastRawRequest
    {
        get
        {
            lock (_gate)
            {
                return _lastRawRequest;
            }
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
    /// Lässt die nächste Anfrage mit dem angegebenen Rohinhalt (Status 200) beantworten, z. B. mit ungültigem JSON.
    /// </summary>
    /// <param name="body">Der Inhalt.</param>
    public void EnqueueBody(string body)
    {
        lock (_gate)
        {
            _queuedBodies.Enqueue(body);
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

    private static string SearchBody(string? query)
    {
        var text = query ?? string.Empty;
        foreach (var place in Places)
        {
            if (text.Contains(place.Key, StringComparison.OrdinalIgnoreCase))
            {
                return "[{\"place_id\":1,\"licence\":\"Data © OpenStreetMap contributors, ODbL 1.0. http://osm.org/copyright\",\"lat\":\""
                    + place.Latitude + "\",\"lon\":\"" + place.Longitude + "\",\"display_name\":\"" + place.DisplayName + "\",\"category\":\"place\",\"type\":\"city\"}]";
            }
        }

        return "[]";
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
        var received = DateTimeOffset.UtcNow;
        var userAgent = context.Request.UserAgent;
        var query = context.Request.QueryString["q"];
        var isSearch = (context.Request.Url?.AbsolutePath ?? string.Empty).EndsWith("/search", StringComparison.Ordinal);
        int? forcedStatus = null;
        string? forcedBody = null;
        lock (_gate)
        {
            if (isSearch)
            {
                _timestamps.Add(received);
                _lastQuery = query;
                _lastUserAgent = userAgent;
                _lastRawRequest = context.Request.Url?.PathAndQuery;
                if (_queuedStatuses.Count > 0)
                {
                    forcedStatus = _queuedStatuses.Dequeue();
                }
                else if (_queuedBodies.Count > 0)
                {
                    forcedBody = _queuedBodies.Dequeue();
                }
            }
        }

        if (ResponseDelay > TimeSpan.Zero)
        {
            await Task.Delay(ResponseDelay).ConfigureAwait(false);
        }

        var status = 200;
        string body;
        if (!isSearch)
        {
            status = 404;
            body = "{}";
        }
        else if (string.IsNullOrWhiteSpace(userAgent))
        {
            // Wie das Original: Anfragen ohne identifizierende Kennung werden abgelehnt.
            status = 403;
            body = "Access blocked: no identifying User-Agent";
        }
        else if (forcedStatus is { } forced)
        {
            status = forced;
            body = "{\"error\":\"erzwungener Fehler\"}";
        }
        else if (forcedBody is not null)
        {
            body = forcedBody;
        }
        else
        {
            body = SearchBody(query);
        }

        var bytes = Encoding.UTF8.GetBytes(body);
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
        context.Response.Close();
    }
}
