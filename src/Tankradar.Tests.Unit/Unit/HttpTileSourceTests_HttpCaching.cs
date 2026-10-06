using System.Net;
using System.Net.Http.Headers;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Services.Map;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der Kachel-Cache die HTTP-Caching-Angaben des Servers beachtet (<c>Cache-Control</c>, <c>Expires</c>, <c>ETag</c>, <c>Last-Modified</c>) und die pauschalen sieben Tage nur als Rückfall gelten.
/// Alle Abrufe laufen gegen einen Test-HTTP-Handler, nie gegen einen echten Kachelserver.
/// </summary>
public class HttpTileSourceTests_HttpCaching : BaseTest
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private static readonly TileKey Key = new(12, 2200, 1343);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "Tankradar.Tests.Unit", Guid.NewGuid().ToString("N"));
    private readonly ManualTimeProvider _clock = new();
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly RecordingLogger<HttpTileSource> _logger = new();

    private HttpTileSource CreateSource()
    {
        return new HttpTileSource(new HttpClient(_handler), new TileServerOptions(), () => _directory, _clock, _logger);
    }

    private static HttpResponseMessage PngResponse(Action<HttpResponseMessage> configure)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Png) };
        configure(response);
        return response;
    }

    private string MetadataPath()
    {
        return Path.Combine(_directory, "12", "2200", "1343.png.meta");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Prüft, dass <c>max-age</c> des Servers die Gültigkeit bestimmt: Innerhalb der Frist kein Abruf, danach ein Abruf (kürzer als die pauschalen sieben Tage).
    /// </summary>
    [Fact]
    public async Task GetTile_MaxAge_OverridesFallbackLifetime()
    {
        _handler.Respond(() => PngResponse(r => r.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromHours(1) }));
        using var first = CreateSource();
        await first.GetTileAsync(Key);

        using var restartedWithin = CreateSource();
        _clock.Advance(TimeSpan.FromMinutes(59));
        await restartedWithin.GetTileAsync(Key);
        Assert.Single(_handler.Requests);

        using var restartedAfter = CreateSource();
        _clock.Advance(TimeSpan.FromMinutes(2));
        await restartedAfter.GetTileAsync(Key);
        Assert.Equal(2, _handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass ohne Angaben des Servers die Rückfallfrist von sieben Tagen gilt.
    /// </summary>
    [Fact]
    public async Task GetTile_WithoutCacheHeaders_UsesSevenDayFallback()
    {
        _handler.Respond(() => PngResponse(_ => { }));
        using var first = CreateSource();
        await first.GetTileAsync(Key);

        _clock.Advance(TimeSpan.FromDays(6));
        using var within = CreateSource();
        await within.GetTileAsync(Key);
        Assert.Single(_handler.Requests);

        _clock.Advance(TimeSpan.FromDays(2));
        using var after = CreateSource();
        await after.GetTileAsync(Key);
        Assert.Equal(2, _handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass <c>Expires</c> ohne <c>Cache-Control</c> gilt (bezogen auf die Serveruhr <c>Date</c>).
    /// </summary>
    [Fact]
    public async Task GetTile_ExpiresHeader_IsHonored()
    {
        _handler.Respond(() => PngResponse(r =>
        {
            var serverNow = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
            r.Headers.Date = serverNow;
            r.Content.Headers.Expires = serverNow.AddHours(2);
        }));
        using var first = CreateSource();
        await first.GetTileAsync(Key);

        _clock.Advance(TimeSpan.FromMinutes(119));
        using var within = CreateSource();
        await within.GetTileAsync(Key);
        Assert.Single(_handler.Requests);

        _clock.Advance(TimeSpan.FromMinutes(2));
        using var after = CreateSource();
        await after.GetTileAsync(Key);
        Assert.Equal(2, _handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass eine abgelaufene Kachel bedingt mit <c>If-None-Match</c> angefragt wird und ein <c>304</c> die gespeicherte Kachel ohne Übertragung bestätigt und verlängert.
    /// </summary>
    [Fact]
    public async Task GetTile_ExpiredWithETag_SendsConditionalRequestAndAccepts304()
    {
        _handler.Respond(() => PngResponse(r =>
        {
            r.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromMinutes(10) };
            r.Headers.ETag = new EntityTagHeaderValue("\"abc\"");
        }));
        _handler.Respond(() => new HttpResponseMessage(HttpStatusCode.NotModified)
        {
            Headers = { CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromMinutes(30) } },
        });
        using (var first = CreateSource())
        {
            await first.GetTileAsync(Key);
        }

        _clock.Advance(TimeSpan.FromMinutes(11));
        using var second = CreateSource();
        var data = await second.GetTileAsync(Key);

        Assert.Equal(Png, data);
        Assert.Equal(2, _handler.Requests.Count);
        Assert.Equal("\"abc\"", _handler.IfNoneMatch[1]);
        Assert.Equal(string.Empty, _handler.IfNoneMatch[0]);

        // Die Bestätigung hat die Gültigkeit um 30 Minuten verlängert; der ETag bleibt erhalten.
        _clock.Advance(TimeSpan.FromMinutes(29));
        using var third = CreateSource();
        Assert.Equal(Png, await third.GetTileAsync(Key));
        Assert.Equal(2, _handler.Requests.Count);
        Assert.Contains("abc", File.ReadAllText(MetadataPath()), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ohne <c>ETag</c> der <c>Last-Modified</c>-Zeitpunkt als <c>If-Modified-Since</c> gesendet wird.
    /// </summary>
    [Fact]
    public async Task GetTile_ExpiredWithLastModified_SendsIfModifiedSince()
    {
        var lastModified = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
        _handler.Respond(() => PngResponse(r =>
        {
            r.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromMinutes(10) };
            r.Content.Headers.LastModified = lastModified;
        }));
        _handler.Respond(() => new HttpResponseMessage(HttpStatusCode.NotModified));
        using (var first = CreateSource())
        {
            await first.GetTileAsync(Key);
        }

        _clock.Advance(TimeSpan.FromMinutes(11));
        using var second = CreateSource();
        Assert.Equal(Png, await second.GetTileAsync(Key));

        Assert.Equal(lastModified.ToString("R"), _handler.IfModifiedSince[1]);
        Assert.Equal(string.Empty, _handler.IfNoneMatch[1]);
    }

    /// <summary>
    /// Prüft, dass eine geänderte Kachel (200 auf die bedingte Anfrage) übernommen wird.
    /// </summary>
    [Fact]
    public async Task GetTile_ExpiredAndChanged_ReplacesTile()
    {
        var other = (byte[])Png.Clone();
        other[^1] ^= 0x01;
        _handler.Respond(() => PngResponse(r =>
        {
            r.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromMinutes(10) };
            r.Headers.ETag = new EntityTagHeaderValue("\"v1\"");
        }));
        _handler.Respond(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(other), Headers = { ETag = new EntityTagHeaderValue("\"v2\"") } });
        using (var first = CreateSource())
        {
            await first.GetTileAsync(Key);
        }

        _clock.Advance(TimeSpan.FromMinutes(11));
        using var second = CreateSource();

        Assert.Equal(other, await second.GetTileAsync(Key));
        Assert.Equal("\"v1\"", _handler.IfNoneMatch[1]);
    }

    /// <summary>
    /// Prüft, dass <c>no-store</c> die Kachel nicht auf dem Gerät speichert, <c>no-cache</c> aber vor jeder erneuten Verwendung eine Rückfrage auslöst.
    /// </summary>
    [Fact]
    public async Task GetTile_NoStoreAndNoCache_AreRespected()
    {
        _handler.Respond(() => PngResponse(r => r.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true }));
        using (var source = CreateSource())
        {
            await source.GetTileAsync(Key);
        }

        Assert.False(File.Exists(Path.Combine(_directory, "12", "2200", "1343.png")));

        var noCache = new FakeHttpMessageHandler();
        noCache.Respond(() => PngResponse(r =>
        {
            r.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            r.Headers.ETag = new EntityTagHeaderValue("\"n\"");
        }));
        noCache.Respond(() => new HttpResponseMessage(HttpStatusCode.NotModified) { Headers = { CacheControl = new CacheControlHeaderValue { NoCache = true } } });
        using var revalidating = new HttpTileSource(new HttpClient(noCache), new TileServerOptions(), () => _directory, _clock, _logger);
        await revalidating.GetTileAsync(Key);
        await revalidating.GetTileAsync(Key);

        Assert.Equal(2, noCache.Requests.Count);
        Assert.Equal("\"n\"", noCache.IfNoneMatch[1]);
    }

    /// <summary>
    /// Prüft, dass bei einem Fehler der Rückfrage die abgelaufene Kachel weiterverwendet wird und dass Kacheln ohne Angaben-Datei (frühere Version) ohne bedingte Anfrage neu geladen werden.
    /// </summary>
    [Fact]
    public async Task GetTile_RevalidationFails_UsesStaleTile()
    {
        _handler.Respond(() => PngResponse(r => r.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromMinutes(1) }));
        _handler.RespondWith(HttpStatusCode.ServiceUnavailable, string.Empty);
        using (var first = CreateSource())
        {
            await first.GetTileAsync(Key);
        }

        _clock.Advance(TimeSpan.FromMinutes(2));
        File.Delete(MetadataPath());
        File.SetLastWriteTimeUtc(Path.Combine(_directory, "12", "2200", "1343.png"), _clock.UtcNow.AddDays(-8));
        using var second = CreateSource();

        Assert.Equal(Png, await second.GetTileAsync(Key));
        Assert.Equal(string.Empty, _handler.IfNoneMatch[1]);
    }

    /// <summary>
    /// Prüft die Auswertung der Kopfzeilen: <c>max-age=0</c> verlangt sofortige Rückfrage, abwegig lange Angaben werden auf ein Jahr begrenzt, negative <c>Expires</c>-Spannen gelten als abgelaufen.
    /// </summary>
    [Fact]
    public void Evaluate_EdgeCases()
    {
        var now = new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
        var fallback = TimeSpan.FromDays(7);

        Assert.Equal(now, TileCachePolicy.Evaluate(new CacheControlHeaderValue { MaxAge = TimeSpan.Zero }, null, null, now, fallback).ExpiresUtc);
        Assert.Equal(now + TileCachePolicy.MaxLifetime, TileCachePolicy.Evaluate(new CacheControlHeaderValue { MaxAge = TimeSpan.FromDays(4000) }, null, null, now, fallback).ExpiresUtc);
        Assert.Equal(now, TileCachePolicy.Evaluate(null, now.AddDays(-1), null, now, fallback).ExpiresUtc);
        Assert.Equal(now + fallback, TileCachePolicy.Evaluate(null, null, null, now, fallback).ExpiresUtc);
        Assert.True(TileCachePolicy.Evaluate(new CacheControlHeaderValue { NoStore = true }, null, null, now, fallback).NoStore);
        Assert.Equal(now, TileCachePolicy.Evaluate(new CacheControlHeaderValue { NoCache = true }, null, null, now, fallback).ExpiresUtc);
    }
}
