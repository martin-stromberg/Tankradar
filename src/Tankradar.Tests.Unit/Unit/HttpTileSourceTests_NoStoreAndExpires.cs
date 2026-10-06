using System.Net;
using System.Net.Http.Headers;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Services.Favorites;
using Tankradar.MAUI.Services.Map;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Randfälle des Kachel-Caches (ungültiges <c>Expires</c>, <c>no-store</c> mit vorhandener Kopie, Altlast im Datenverzeichnis) und die Übergabe der Favoriten-Änderung an den Kontext der Anmeldung.
/// </summary>
public class HttpTileSourceTests_NoStoreAndExpires : BaseTest
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private static readonly TileKey Key = new(12, 2200, 1343);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "Tankradar.Tests.Unit", Guid.NewGuid().ToString("N"));
    private readonly ManualTimeProvider _clock = new();
    private readonly RecordingLogger<HttpTileSource> _logger = new();

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
    /// Prüft, dass ein ungültiges <c>Expires</c> (z. B. „0“) als bereits abgelaufen gilt und nicht als fehlende Angabe mit sieben Tagen.
    /// </summary>
    [Fact]
    public async Task GetTile_InvalidExpires_IsTreatedAsExpired()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Respond(() =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Png) };
            response.Content.Headers.TryAddWithoutValidation("Expires", "0");
            return response;
        });
        using (var first = new HttpTileSource(new HttpClient(handler), new TileServerOptions(), () => _directory, _clock, _logger))
        {
            await first.GetTileAsync(Key);
        }

        using var second = new HttpTileSource(new HttpClient(handler), new TileServerOptions(), () => _directory, _clock, _logger);
        await second.GetTileAsync(Key);

        Assert.Equal(2, handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass eine frühere Kopie samt Angaben vom Gerät verschwindet, wenn der Server später <c>no-store</c> verlangt.
    /// </summary>
    [Fact]
    public async Task GetTile_NoStoreAfterEarlierCopy_RemovesStoredFiles()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Respond(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Png),
            Headers = { CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromMinutes(1) }, ETag = new EntityTagHeaderValue("\"e\"") },
        });
        handler.Respond(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Png),
            Headers = { CacheControl = new CacheControlHeaderValue { NoStore = true } },
        });
        using (var first = new HttpTileSource(new HttpClient(handler), new TileServerOptions(), () => _directory, _clock, _logger))
        {
            await first.GetTileAsync(Key);
        }

        Assert.True(File.Exists(Path.Combine(_directory, "12", "2200", "1343.png.meta")));
        _clock.Advance(TimeSpan.FromMinutes(2));
        using var second = new HttpTileSource(new HttpClient(handler), new TileServerOptions(), () => _directory, _clock, _logger);
        await second.GetTileAsync(Key);

        Assert.False(File.Exists(Path.Combine(_directory, "12", "2200", "1343.png")));
        Assert.False(File.Exists(Path.Combine(_directory, "12", "2200", "1343.png.meta")));
    }

    /// <summary>
    /// Prüft, dass der Kachelspeicher früherer Versionen im Datenverzeichnis gelöscht wird, das aktuelle Verzeichnis aber nie.
    /// </summary>
    [Fact]
    public void LegacyTileCache_DeletesOldDirectoryOnly()
    {
        var legacy = Path.Combine(_directory, "data", "tiles");
        var current = Path.Combine(_directory, "cache", "tiles");
        Directory.CreateDirectory(legacy);
        Directory.CreateDirectory(current);
        File.WriteAllBytes(Path.Combine(legacy, "x.png"), Png);

        Assert.True(LegacyTileCache.Delete(legacy, current));
        Assert.False(Directory.Exists(legacy));
        Assert.True(Directory.Exists(current));
        Assert.False(LegacyTileCache.Delete(legacy, current));
        Assert.False(LegacyTileCache.Delete(current, current));
        Assert.True(Directory.Exists(current));
    }

    /// <summary>
    /// Prüft, dass die Reaktion auf eine Änderung im Kontext der Anmeldung (UI-Thread) ausgeführt wird, wenn das Ereignis von einem anderen Thread kommt.
    /// </summary>
    [Fact]
    public async Task FavoritesChangeSubscription_PostsToSubscriberContext()
    {
        using var fixture = new FavoritesFixture();
        var context = new RecordingContext();
        var owner = new object();
        var calls = 0;
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            FavoritesChangeSubscription.Attach(fixture.Service, owner, _ => calls++);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        await Task.Run(() => fixture.Service.CreateGroupAsync("Egal", null));

        Assert.Equal(1, context.Posted);
        Assert.Equal(1, calls);
        GC.KeepAlive(owner);
    }

    private sealed class RecordingContext : SynchronizationContext
    {
        public int Posted { get; private set; }

        public override void Post(SendOrPostCallback d, object? state)
        {
            Posted++;
            d(state);
        }
    }
}
