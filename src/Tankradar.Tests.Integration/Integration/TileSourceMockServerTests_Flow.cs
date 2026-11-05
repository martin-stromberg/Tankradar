using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Services.Map;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Integrationstest: Kachelquelle gegen den lokalen Mock-Kachelserver (echtes HTTP über Loopback, nie ein produktiver Kachelserver).
/// </summary>
public class TileSourceMockServerTests_Flow : IDisposable
{
    private readonly MockTileServer _server = new();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "Tankradar.Tests.Integration", Guid.NewGuid().ToString("N"));
    private readonly HttpClient _client = new(new HttpClientHandler { AllowAutoRedirect = false });

    private HttpTileSource CreateSource()
    {
        var options = new TileServerOptions { UrlTemplate = _server.BaseUrl + "{z}/{x}/{y}.png", AllowLoopbackHttp = true };
        options.Validate();
        return new HttpTileSource(_client, options, () => _directory, TimeProvider.System, new RecordingLogger<HttpTileSource>());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Prüft, dass die Kachel mit identifizierender Kennung abgerufen, als PNG geliefert und beim zweiten Zugriff aus dem Speicher bedient wird.
    /// </summary>
    [Fact]
    public async Task GetTile_FetchesOnceWithUserAgentAndCaches()
    {
        using var source = CreateSource();

        var first = await source.GetTileAsync(new TileKey(12, 2200, 1343));
        var second = await source.GetTileAsync(new TileKey(12, 2200, 1343));

        Assert.NotNull(first);
        Assert.Equal(first, second);
        Assert.Equal(["/12/2200/1343.png"], _server.Paths);
        Assert.Contains("Tankatlas", _server.UserAgents.Single(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ein Fehlerstatus des Servers zu <see langword="null"/> führt und innerhalb des Wartefensters nicht wiederholt wird.
    /// </summary>
    [Fact]
    public async Task GetTile_ServerError_ReturnsNullWithoutRepeating()
    {
        _server.EnqueueStatuses(500);
        using var source = CreateSource();

        Assert.Null(await source.GetTileAsync(new TileKey(5, 3, 3)));
        Assert.Null(await source.GetTileAsync(new TileKey(5, 3, 3)));

        Assert.Equal(1, _server.Requests);
    }
}
