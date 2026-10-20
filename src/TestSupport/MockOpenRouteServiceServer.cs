using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Tankradar.TestSupport;

/// <summary>
/// Lokaler Mock von OpenRouteService (Directions, Profil Auto) auf einer Loopback-Adresse (ausschließlich für Tests; kein produktiver Endpunkt).
/// Beantwortet <c>POST v2/directions/driving-car</c> mit einer fest vorgegebenen oder aus Start und Ziel abgeleiteten Route als kodierter Polylinie,
/// verlangt wie das Original den Schlüssel im Kopf <c>Authorization</c> und lässt sich zu Fehlerantworten und Verzögerungen anweisen.
/// </summary>
public sealed class MockOpenRouteServiceServer : IDisposable
{
    /// <summary>
    /// Der Schlüssel, den der Mock akzeptiert.
    /// </summary>
    public const string AcceptedKey = "mock-route-key-for-tests";

    private readonly HttpListener _listener;
    private readonly object _gate = new();
    private readonly Queue<(int Status, int? Code)> _queuedErrors = new();
    private readonly List<DateTimeOffset> _timestamps = [];
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _loop;
    private IReadOnlyList<(double Latitude, double Longitude)>? _route;
    private int _disposed;
    private string? _lastBody;
    private string? _lastAuthorization;
    private string? _lastRawRequest;
    private string? _lastUserAgent;

    /// <summary>
    /// Startet den Server auf einem freien Port.
    /// </summary>
    public MockOpenRouteServiceServer()
    {
        _listener = LoopbackHttpListener.Start(out var port);
        BaseUrl = new Uri($"http://127.0.0.1:{port}/");
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
    /// Anzahl der empfangenen Routenanfragen.
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
    /// Die Empfangszeitpunkte aller Routenanfragen in Reihenfolge.
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
    /// Der Inhalt der zuletzt empfangenen Anfrage.
    /// </summary>
    public string? LastRequestBody
    {
        get
        {
            lock (_gate)
            {
                return _lastBody;
            }
        }
    }

    /// <summary>
    /// Der Kopf <c>Authorization</c> der zuletzt empfangenen Anfrage.
    /// </summary>
    public string? LastAuthorization
    {
        get
        {
            lock (_gate)
            {
                return _lastAuthorization;
            }
        }
    }

    /// <summary>
    /// Pfad und Query der zuletzt empfangenen Anfrage (zum Prüfen, dass der Schlüssel nie in der Adresse steht).
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
    /// Legt die Route fest, die der Mock liefert; ohne Vorgabe verbindet er Start und Ziel der Anfrage geradlinig.
    /// </summary>
    /// <param name="points">Die Punkte der Route (Breite, Länge), mindestens zwei.</param>
    public void SetRoute(IReadOnlyList<(double Latitude, double Longitude)> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        lock (_gate)
        {
            _route = points.ToArray();
        }
    }

    /// <summary>
    /// Lässt die nächsten Anfragen mit den angegebenen HTTP-Statuscodes beantworten (je Anfrage einer; ohne ORS-Fehlercode).
    /// </summary>
    /// <param name="statuses">Die Statuscodes in Reihenfolge.</param>
    public void EnqueueStatuses(params int[] statuses)
    {
        lock (_gate)
        {
            foreach (var status in statuses)
            {
                _queuedErrors.Enqueue((status, null));
            }
        }
    }

    /// <summary>
    /// Lässt die nächste Anfrage mit einem Status und einem Fehlercode im Format von OpenRouteService beantworten (<c>{"error":{"code":…}}</c>).
    /// </summary>
    /// <param name="status">Der HTTP-Statuscode.</param>
    /// <param name="code">Der Fehlercode von OpenRouteService, z. B. 2004 (Strecke zu lang) oder 2010 (Punkt nicht routbar).</param>
    public void EnqueueError(int status, int code)
    {
        lock (_gate)
        {
            _queuedErrors.Enqueue((status, code));
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

    private static IReadOnlyList<(double Latitude, double Longitude)>? ParseCoordinates(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("coordinates", out var coordinates) || coordinates.GetArrayLength() < 2)
            {
                return null;
            }

            var result = new List<(double, double)>();
            foreach (var pair in coordinates.EnumerateArray())
            {
                result.Add((pair[1].GetDouble(), pair[0].GetDouble()));
            }

            return result;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or IndexOutOfRangeException)
        {
            return null;
        }
    }

    private static List<(double Latitude, double Longitude)> Interpolate((double Latitude, double Longitude) from, (double Latitude, double Longitude) to)
    {
        const int Segments = 20;
        var points = new List<(double, double)>();
        for (var i = 0; i <= Segments; i++)
        {
            var t = i / (double)Segments;
            points.Add((from.Latitude + (t * (to.Latitude - from.Latitude)), from.Longitude + (t * (to.Longitude - from.Longitude))));
        }

        return points;
    }

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2))
            + (Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2));
        return 6371.0 * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private string RouteBody(string requestBody)
    {
        IReadOnlyList<(double Latitude, double Longitude)>? points;
        lock (_gate)
        {
            points = _route;
        }

        if (points is null)
        {
            var coordinates = ParseCoordinates(requestBody);
            if (coordinates is null)
            {
                return "{\"error\":{\"code\":2003,\"message\":\"Parameter 'coordinates' fehlt\"}}";
            }

            points = Interpolate(coordinates[0], coordinates[^1]);
        }

        var distanceMeters = 0.0;
        for (var i = 1; i < points.Count; i++)
        {
            distanceMeters += HaversineKm(points[i - 1].Latitude, points[i - 1].Longitude, points[i].Latitude, points[i].Longitude) * 1000;
        }

        var geometry = PolylineEncoder.Encode(points);
        return "{\"routes\":[{\"summary\":{\"distance\":" + distanceMeters.ToString("0.0", CultureInfo.InvariantCulture)
            + ",\"duration\":" + (distanceMeters / 25).ToString("0.0", CultureInfo.InvariantCulture)
            + "},\"geometry\":\"" + geometry.Replace("\\", "\\\\", StringComparison.Ordinal) + "\"}],\"metadata\":{\"service\":\"routing\"}}";
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
        var isDirections = context.Request.HttpMethod == "POST"
            && (context.Request.Url?.AbsolutePath ?? string.Empty).EndsWith("/v2/directions/driving-car", StringComparison.Ordinal);
        string requestBody;
        using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
        {
            requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        (int Status, int? Code)? forced = null;
        lock (_gate)
        {
            if (isDirections)
            {
                _timestamps.Add(received);
                _lastBody = requestBody;
                _lastAuthorization = context.Request.Headers["Authorization"];
                _lastRawRequest = context.Request.Url?.PathAndQuery;
                _lastUserAgent = context.Request.UserAgent;
                if (_queuedErrors.Count > 0)
                {
                    forced = _queuedErrors.Dequeue();
                }
            }
        }

        if (ResponseDelay > TimeSpan.Zero)
        {
            await Task.Delay(ResponseDelay).ConfigureAwait(false);
        }

        var status = 200;
        string body;
        if (!isDirections)
        {
            status = 404;
            body = "{\"error\":{\"code\":404,\"message\":\"Unbekannter Pfad\"}}";
        }
        else if (forced is { } error)
        {
            status = error.Status;
            body = error.Code is { } code
                ? "{\"error\":{\"code\":" + code.ToString(CultureInfo.InvariantCulture) + ",\"message\":\"erzwungener Fehler\"}}"
                : "{\"error\":\"erzwungener Fehler\"}";
        }
        else if (context.Request.Headers["Authorization"] != AcceptedKey)
        {
            status = 403;
            body = "{\"error\":\"Access to this API has been disallowed\"}";
        }
        else
        {
            body = RouteBody(requestBody);
            if (body.StartsWith("{\"error\"", StringComparison.Ordinal))
            {
                status = 400;
            }
        }

        var bytes = Encoding.UTF8.GetBytes(body);
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
        context.Response.Close();
    }
}
