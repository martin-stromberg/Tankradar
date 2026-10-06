using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Tankradar.TestSupport;

/// <summary>
/// Lokaler Mock eines Kachelservers von OpenStreetMap auf einer Loopback-Adresse (ausschließlich für Tests; kein produktiver Endpunkt).
/// Beantwortet <c>/{z}/{x}/{y}.png</c> mit einer einfarbigen PNG-Kachel, verlangt wie das Original eine identifizierende Kennung (<c>User-Agent</c>),
/// protokolliert Anfragen und lässt sich zu Fehlerantworten anweisen.
/// </summary>
public sealed class MockTileServer : IDisposable
{
    private static readonly Regex TilePath = new(@"^/(?<z>\d{1,2})/(?<x>\d+)/(?<y>\d+)\.png$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Gültige 1x1-PNG-Datei (graublau); die Karte streckt sie auf die Kachelgröße.
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private readonly HttpListener _listener = new();
    private readonly object _gate = new();
    private readonly List<string> _paths = [];
    private readonly List<string> _userAgents = [];
    private readonly Queue<int> _queuedStatuses = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _loop;
    private int _disposed;

    /// <summary>
    /// Startet den Server auf einem freien Port.
    /// </summary>
    public MockTileServer()
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
    /// Anzahl der empfangenen Kachelanfragen.
    /// </summary>
    public int Requests
    {
        get
        {
            lock (_gate)
            {
                return _paths.Count;
            }
        }
    }

    /// <summary>
    /// Die Pfade aller empfangenen Kachelanfragen in Reihenfolge (<c>/z/x/y.png</c>).
    /// </summary>
    public IReadOnlyList<string> Paths
    {
        get
        {
            lock (_gate)
            {
                return _paths.ToArray();
            }
        }
    }

    /// <summary>
    /// Die <c>User-Agent</c>-Kennungen aller empfangenen Kachelanfragen (leer, wenn keine gesetzt war).
    /// </summary>
    public IReadOnlyList<string> UserAgents
    {
        get
        {
            lock (_gate)
            {
                return _userAgents.ToArray();
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

            try
            {
                await HandleAsync(context).ConfigureAwait(false);
            }
            catch (Exception)
            {
                context.Response.Abort();
            }
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        var path = context.Request.Url?.AbsolutePath ?? string.Empty;
        var userAgent = context.Request.UserAgent ?? string.Empty;
        int? forcedStatus = null;
        var isTile = TilePath.IsMatch(path);
        lock (_gate)
        {
            if (isTile)
            {
                _paths.Add(path);
                _userAgents.Add(userAgent);
                if (_queuedStatuses.Count > 0)
                {
                    forcedStatus = _queuedStatuses.Dequeue();
                }
            }
        }

        if (!isTile)
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
            return;
        }

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            // Wie das Original: Anfragen ohne identifizierende Kennung werden abgelehnt.
            context.Response.StatusCode = 403;
            context.Response.Close();
            return;
        }

        if (forcedStatus is { } status)
        {
            context.Response.StatusCode = status;
            context.Response.Close();
            return;
        }

        context.Response.StatusCode = 200;
        context.Response.ContentType = "image/png";
        context.Response.ContentLength64 = Png.Length;
        await context.Response.OutputStream.WriteAsync(Png).ConfigureAwait(false);
        context.Response.Close();
    }
}
