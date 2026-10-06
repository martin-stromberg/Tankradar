using System.Net;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Services.Map;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Abruf und die Zwischenspeicherung von Kartenkacheln gegen einen Test-HTTP-Handler (nie gegen einen echten Kachelserver):
/// Kennung, Zwischenspeicher im Speicher und auf dem Gerät, Ablauf, veraltete Kacheln bei Fehlern, Wartefenster nach Fehlern und Ablehnung ungültiger Antworten.
/// </summary>
public class HttpTileSourceTests_Fetch : BaseTest
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private static readonly TileKey Key = new(12, 2200, 1343);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "Tankradar.Tests.Unit", Guid.NewGuid().ToString("N"));
    private readonly ManualTimeProvider _clock = new();
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly RecordingLogger<HttpTileSource> _logger = new();

    private HttpTileSource CreateSource(TileServerOptions? options = null)
    {
        return new HttpTileSource(new HttpClient(_handler), options ?? new TileServerOptions(), () => _directory, _clock, _logger);
    }

    private FakeHttpMessageHandler RespondWithPng()
    {
        return _handler.RespondWithBytes(HttpStatusCode.OK, Png);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var directory in new[] { _directory, _directory + "-slow", _directory + "-big" }.Where(Directory.Exists))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Prüft, dass eine Kachel mit identifizierender Kennung vom eingestellten Server abgerufen wird.
    /// </summary>
    [Fact]
    public async Task GetTile_Downloads_WithUserAgentAndCorrectUrl()
    {
        RespondWithPng();
        using var source = CreateSource();

        var data = await source.GetTileAsync(Key);

        Assert.Equal(Png, data);
        Assert.Equal([new Uri("https://tile.openstreetmap.org/12/2200/1343.png")], _handler.Requests);
        Assert.Contains("Tankatlas", _handler.UserAgents.Single(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass eine bereits abgerufene Kachel aus dem Speicher geliefert wird und nach einem Neustart (neue Instanz) vom Gerät, jeweils ohne neue Anfrage.
    /// </summary>
    [Fact]
    public async Task GetTile_SecondRequestAndRestart_UseCache()
    {
        RespondWithPng();
        using (var first = CreateSource())
        {
            await first.GetTileAsync(Key);
            await first.GetTileAsync(Key);
        }

        using var restarted = CreateSource();
        var data = await restarted.GetTileAsync(Key);

        Assert.Equal(Png, data);
        Assert.Single(_handler.Requests);
    }

    /// <summary>
    /// Prüft, dass eine nach sieben Tagen abgelaufene Kachel erneut abgerufen wird, bei einem Fehler aber die veraltete Kachel weiterverwendet wird.
    /// </summary>
    [Fact]
    public async Task GetTile_ExpiredCache_RefetchesAndFallsBackToStaleOnFailure()
    {
        RespondWithPng();
        using (var first = CreateSource())
        {
            await first.GetTileAsync(Key);
        }

        File.SetLastWriteTimeUtc(Path.Combine(_directory, "12", "2200", "1343.png"), _clock.UtcNow.AddDays(-8));
        _handler.RespondWith(HttpStatusCode.ServiceUnavailable, string.Empty);
        using var restarted = CreateSource();

        var data = await restarted.GetTileAsync(Key);

        Assert.Equal(Png, data);
        Assert.Equal(2, _handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass nach einem Fehler innerhalb des Wartefensters keine weitere Anfrage ausgelöst wird, danach aber wieder.
    /// </summary>
    [Fact]
    public async Task GetTile_AfterFailure_WaitsBeforeRetry()
    {
        _handler.RespondWith(HttpStatusCode.InternalServerError, string.Empty);
        using var source = CreateSource();

        Assert.Null(await source.GetTileAsync(Key));
        Assert.Null(await source.GetTileAsync(Key));
        Assert.Single(_handler.Requests);

        _clock.Advance(TimeSpan.FromSeconds(31));
        RespondWithPng();

        Assert.Equal(Png, await source.GetTileAsync(Key));
        Assert.Equal(2, _handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass Netzwerkfehler und Zeitüberschreitungen zu <see langword="null"/> führen (die Karte bleibt benutzbar) und keine Kachelnummern ins Protokoll gelangen.
    /// </summary>
    [Fact]
    public async Task GetTile_NetworkErrorAndTimeout_ReturnNullWithoutLoggingTileNumbers()
    {
        _handler.Throw(new HttpRequestException("kein Netz 2200/1343"));
        using var source = CreateSource();
        Assert.Null(await source.GetTileAsync(Key));

        var hanging = new FakeHttpMessageHandler().Hang();
        using var slow = new HttpTileSource(new HttpClient(hanging), new TileServerOptions { RequestTimeout = TimeSpan.FromMilliseconds(50) }, () => _directory + "-slow", _clock, _logger);
        Assert.Null(await slow.GetTileAsync(new TileKey(5, 1, 1)));

        Assert.DoesNotContain("2200", _logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("1343", _logger.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ein Abbruch durch den Aufrufer als Abbruch weitergegeben wird.
    /// </summary>
    [Fact]
    public async Task GetTile_CallerCancellation_Propagates()
    {
        _handler.Hang();
        using var source = CreateSource();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.GetTileAsync(Key, cancellation.Token));
    }

    /// <summary>
    /// Prüft, dass Antworten, die keine PNG-Datei sind oder zu groß ausfallen, abgelehnt und nicht gespeichert werden.
    /// </summary>
    [Fact]
    public async Task GetTile_InvalidOrOversizedResponse_IsRejected()
    {
        _handler.RespondWithBytes(HttpStatusCode.OK, [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
        using var source = CreateSource();
        Assert.Null(await source.GetTileAsync(Key));

        var large = new byte[2048];
        Png.CopyTo(large, 0);
        var bigHandler = new FakeHttpMessageHandler().RespondWithBytes(HttpStatusCode.OK, large);
        using var limited = new HttpTileSource(new HttpClient(bigHandler), new TileServerOptions { MaxTileBytes = 1024 }, () => _directory + "-big", _clock, _logger);
        Assert.Null(await limited.GetTileAsync(Key));
        Assert.False(File.Exists(Path.Combine(_directory, "12", "2200", "1343.png")));
    }

    /// <summary>
    /// Prüft, dass im Testmodus ohne Endpunkt und für ungültige Kachelnummern nie eine Anfrage entsteht.
    /// </summary>
    [Fact]
    public async Task GetTile_NotConfiguredOrInvalidKey_NeverRequests()
    {
        RespondWithPng();
        using var refused = CreateSource(new TileServerOptions { EndpointNotConfigured = true });
        using var source = CreateSource();

        Assert.Null(await refused.GetTileAsync(Key));
        Assert.Null(await source.GetTileAsync(new TileKey(3, 8, 0)));
        Assert.Null(await source.GetTileAsync(new TileKey(3, 0, -1)));
        Assert.Null(await source.GetTileAsync(new TileKey(30, 0, 0)));
        Assert.Empty(_handler.Requests);
    }
}
