using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Services.Map;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der Kachel-Cache die Angabe <c>Age</c> beachtet und ein <c>304</c> ohne eigene Caching-Angaben die gespeicherte Lebensdauer weiter gelten lässt.
/// Alle Abrufe laufen gegen einen Test-HTTP-Handler, nie gegen einen echten Kachelserver.
/// </summary>
public class HttpTileSourceTests_AgeAndRevalidation : BaseTest
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

    private static HttpResponseMessage MaxAgeResponse(TimeSpan maxAge, TimeSpan? age = null)
    {
        return PngResponse(r =>
        {
            r.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = maxAge };
            r.Headers.ETag = new EntityTagHeaderValue("\"abc\"");
            r.Headers.Age = age;
        });
    }

    private async Task GetAfterAsync(TimeSpan advance)
    {
        _clock.Advance(advance);
        using var source = CreateSource();
        Assert.Equal(Png, await source.GetTileAsync(Key));
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
    /// Prüft, dass <c>Age</c> die Gültigkeit kürzt: <c>max-age=600</c> und <c>Age: 540</c> gelten nur noch eine Minute.
    /// </summary>
    [Fact]
    public async Task GetTile_AgeHeader_ReducesFreshness()
    {
        _handler.Respond(() => MaxAgeResponse(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(540)));
        await GetAfterAsync(TimeSpan.Zero);

        await GetAfterAsync(TimeSpan.FromSeconds(50));
        Assert.Single(_handler.Requests);

        await GetAfterAsync(TimeSpan.FromSeconds(20));
        Assert.Equal(2, _handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass eine 304 ohne Caching-Angaben die gespeicherte Lebensdauer (10 Minuten) statt pauschal sieben Tage verwendet.
    /// </summary>
    [Fact]
    public async Task GetTile_NotModifiedWithoutCachingHeaders_KeepsPreviousLifetime()
    {
        _handler.Respond(() => MaxAgeResponse(TimeSpan.FromMinutes(10)));
        _handler.Respond(() => new HttpResponseMessage(HttpStatusCode.NotModified));
        await GetAfterAsync(TimeSpan.Zero);

        await GetAfterAsync(TimeSpan.FromMinutes(11));
        Assert.Equal(2, _handler.Requests.Count);

        await GetAfterAsync(TimeSpan.FromMinutes(9));
        Assert.Equal(2, _handler.Requests.Count);

        await GetAfterAsync(TimeSpan.FromMinutes(2));
        Assert.Equal(3, _handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass eine 304 mit eigenen Angaben die neue Lebensdauer verwendet und speichert.
    /// </summary>
    [Fact]
    public async Task GetTile_NotModifiedWithOwnHeaders_UsesNewLifetime()
    {
        _handler.Respond(() => MaxAgeResponse(TimeSpan.FromMinutes(10)));
        _handler.Respond(() => new HttpResponseMessage(HttpStatusCode.NotModified) { Headers = { CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromMinutes(30) } } });
        await GetAfterAsync(TimeSpan.Zero);
        await GetAfterAsync(TimeSpan.FromMinutes(11));

        await GetAfterAsync(TimeSpan.FromMinutes(29));

        Assert.Equal(2, _handler.Requests.Count);
        Assert.Equal(TimeSpan.FromMinutes(30), JsonSerializer.Deserialize<TileMetadata>(File.ReadAllText(MetadataPath()))!.Lifetime);
    }

    /// <summary>
    /// Prüft, dass Altbestand ohne gespeicherte Lebensdauer bei einer 304 ohne Angaben die Rückfallfrist erhält.
    /// </summary>
    [Fact]
    public async Task GetTile_NotModifiedLegacyMetadata_UsesFallback()
    {
        _handler.Respond(() => MaxAgeResponse(TimeSpan.FromMinutes(10)));
        _handler.Respond(() => new HttpResponseMessage(HttpStatusCode.NotModified));
        await GetAfterAsync(TimeSpan.Zero);
        var legacy = JsonSerializer.Deserialize<TileMetadata>(File.ReadAllText(MetadataPath()))! with { Lifetime = null };
        File.WriteAllText(MetadataPath(), JsonSerializer.Serialize(legacy));

        await GetAfterAsync(TimeSpan.FromMinutes(11));
        await GetAfterAsync(TimeSpan.FromDays(6));

        Assert.Equal(2, _handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass nach <c>no-cache</c> auch eine 304 ohne Angaben vor jeder Verwendung erneut rückfragen lässt.
    /// </summary>
    [Fact]
    public async Task GetTile_NotModifiedAfterNoCache_RevalidatesAgain()
    {
        _handler.Respond(() => PngResponse(r =>
        {
            r.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            r.Headers.ETag = new EntityTagHeaderValue("\"abc\"");
        }));
        _handler.Respond(() => new HttpResponseMessage(HttpStatusCode.NotModified));
        await GetAfterAsync(TimeSpan.Zero);

        await GetAfterAsync(TimeSpan.FromMinutes(1));
        await GetAfterAsync(TimeSpan.FromMinutes(1));

        Assert.Equal(3, _handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass das <c>Age</c> einer 304 die gespeicherte Lebensdauer kürzt.
    /// </summary>
    [Fact]
    public async Task GetTile_NotModifiedWithAge_ReducesPreviousLifetime()
    {
        _handler.Respond(() => MaxAgeResponse(TimeSpan.FromMinutes(10)));
        _handler.Respond(() => new HttpResponseMessage(HttpStatusCode.NotModified) { Headers = { Age = TimeSpan.FromMinutes(4) } });
        await GetAfterAsync(TimeSpan.Zero);
        await GetAfterAsync(TimeSpan.FromMinutes(11));

        await GetAfterAsync(TimeSpan.FromMinutes(5));
        Assert.Equal(2, _handler.Requests.Count);

        await GetAfterAsync(TimeSpan.FromMinutes(2));
        Assert.Equal(3, _handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass Angaben ohne <c>Lifetime</c> (Altbestand) lesbar sind und das Feld dann <see langword="null"/> ist.
    /// </summary>
    [Fact]
    public void Metadata_WithoutLifetime_IsReadable()
    {
        var json = """{"ETag":"\"a\"","LastModified":null,"ExpiresUtc":"2026-10-01T12:00:00+00:00"}""";

        var metadata = JsonSerializer.Deserialize<TileMetadata>(json);

        Assert.NotNull(metadata);
        Assert.Null(metadata.Lifetime);
        Assert.Equal("\"a\"", metadata.ETag);
    }
}
