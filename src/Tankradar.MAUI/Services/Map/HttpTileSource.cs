using System.Globalization;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Map;

namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Ruft Kartenkacheln per HTTP ab und hält sie im Arbeitsspeicher und auf dem Gerät vor. Die Klasse hält die Nutzungsrichtlinie der OpenStreetMap-Kachelserver ein:
/// identifizierende Kennung, höchstens zwei parallele Abrufe, mindestens sieben Tage Zwischenspeicherung, keine Wiederholung nach Fehlern innerhalb des Wartefensters,
/// keine Weiterleitungen und keine Vorabrufe. Koordinaten oder Kachelnummern werden nie protokolliert.
/// </summary>
public sealed class HttpTileSource : ITileSource, IDisposable
{
    private const int MemoryCapacity = 256;
    private const int PruneEveryWrites = 50;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly HttpClient _client;
    private readonly TileServerOptions _options;
    private readonly Func<string> _cacheDirectory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HttpTileSource> _logger;
    private readonly SemaphoreSlim _slots;
    private readonly object _gate = new();
    private readonly Dictionary<TileKey, byte[]> _memory = [];
    private readonly Queue<TileKey> _memoryOrder = new();
    private readonly Dictionary<TileKey, DateTimeOffset> _failures = [];
    private int _writes;

    /// <summary>
    /// Erstellt die Kachelquelle.
    /// </summary>
    /// <param name="client">Der HTTP-Client (ohne automatische Weiterleitungen).</param>
    /// <param name="options">Die Einstellungen.</param>
    /// <param name="cacheDirectory">Liefert das Verzeichnis des Kachelspeichers auf dem Gerät.</param>
    /// <param name="timeProvider">Die Zeitquelle.</param>
    /// <param name="logger">Logger (protokolliert nie Kachelnummern).</param>
    public HttpTileSource(HttpClient client, TileServerOptions options, Func<string> cacheDirectory, TimeProvider timeProvider, ILogger<HttpTileSource> logger)
    {
        _client = client;
        _options = options;
        _cacheDirectory = cacheDirectory;
        _timeProvider = timeProvider;
        _logger = logger;
        _slots = new SemaphoreSlim(options.MaxConcurrentRequests, options.MaxConcurrentRequests);
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetTileAsync(TileKey key, CancellationToken cancellationToken = default)
    {
        if (_options.EndpointNotConfigured || !IsValid(key))
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        if (TryGetMemory(key, out var cached))
        {
            // Der Arbeitsspeicher gilt nur für diese Sitzung; Alter gegenüber der Datei wird dort geprüft, daher genügt der Treffer.
            return cached;
        }

        var stale = ReadFromDisk(key, now, out var fresh);
        if (fresh is not null)
        {
            Remember(key, fresh);
            return fresh;
        }

        if (IsInBackoff(key, now))
        {
            return stale;
        }

        var downloaded = await DownloadAsync(key, cancellationToken).ConfigureAwait(false);
        if (downloaded is null)
        {
            RememberFailure(key, now);
            return stale;
        }

        Remember(key, downloaded);
        WriteToDisk(key, downloaded);
        return downloaded;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _slots.Dispose();
    }

    private static bool IsValid(TileKey key)
    {
        if (key.Zoom is < 0 or > 22)
        {
            return false;
        }

        var count = 1L << key.Zoom;
        return key.X >= 0 && key.X < count && key.Y >= 0 && key.Y < count;
    }

    private static bool LooksLikePng(byte[] data)
    {
        return data.Length > PngSignature.Length && data.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature);
    }

    private async Task<byte[]?> DownloadAsync(TileKey key, CancellationToken cancellationToken)
    {
        await _slots.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_options.RequestTimeout);
            using var request = new HttpRequestMessage(HttpMethod.Get, _options.CreateUri(key));
            request.Headers.UserAgent.ParseAdd(_options.UserAgent);
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Eine Kachel konnte nicht abgerufen werden (Status {Status}).", (int)response.StatusCode);
                return null;
            }

            if (response.Content.Headers.ContentLength is { } length && length > _options.MaxTileBytes)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var chunk = new byte[16 * 1024];
            int read;
            while ((read = await stream.ReadAsync(chunk, timeout.Token).ConfigureAwait(false)) > 0)
            {
                buffer.Write(chunk, 0, read);
                if (buffer.Length > _options.MaxTileBytes)
                {
                    return null;
                }
            }

            var data = buffer.ToArray();
            return LooksLikePng(data) ? data : null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Der Abruf einer Kachel hat das Zeitlimit überschritten.");
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug("Eine Kachel konnte nicht abgerufen werden ({ExceptionType}).", ex.GetType().Name);
            return null;
        }
        finally
        {
            _slots.Release();
        }
    }

    private bool TryGetMemory(TileKey key, out byte[]? data)
    {
        lock (_gate)
        {
            return _memory.TryGetValue(key, out data);
        }
    }

    private void Remember(TileKey key, byte[] data)
    {
        lock (_gate)
        {
            if (_memory.TryAdd(key, data))
            {
                _memoryOrder.Enqueue(key);
                while (_memory.Count > MemoryCapacity && _memoryOrder.TryDequeue(out var oldest))
                {
                    _memory.Remove(oldest);
                }
            }

            _failures.Remove(key);
        }
    }

    private void RememberFailure(TileKey key, DateTimeOffset now)
    {
        lock (_gate)
        {
            foreach (var expired in _failures.Where(entry => entry.Value <= now).Select(entry => entry.Key).ToList())
            {
                _failures.Remove(expired);
            }

            _failures[key] = now + _options.FailureBackoff;
        }
    }

    private bool IsInBackoff(TileKey key, DateTimeOffset now)
    {
        lock (_gate)
        {
            return _failures.TryGetValue(key, out var until) && now < until;
        }
    }

    private string PathOf(TileKey key)
    {
        return Path.Combine(
            _cacheDirectory(),
            key.Zoom.ToString(CultureInfo.InvariantCulture),
            key.X.ToString(CultureInfo.InvariantCulture),
            key.Y.ToString(CultureInfo.InvariantCulture) + ".png");
    }

    private byte[]? ReadFromDisk(TileKey key, DateTimeOffset now, out byte[]? fresh)
    {
        fresh = null;
        try
        {
            var path = PathOf(key);
            if (!File.Exists(path))
            {
                return null;
            }

            var data = File.ReadAllBytes(path);
            if (!LooksLikePng(data))
            {
                return null;
            }

            var age = now - new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);
            if (age < _options.CacheLifetime)
            {
                fresh = data;
            }

            return data;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Der Kachelspeicher ist eine reine Beschleunigung; ein Lesefehler führt zum Abruf.
            return null;
        }
    }

    private void WriteToDisk(TileKey key, byte[] data)
    {
        try
        {
            var path = PathOf(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllBytes(temp, data);
            File.Move(temp, path, overwrite: true);
            if (Interlocked.Increment(ref _writes) % PruneEveryWrites == 0)
            {
                Prune();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug("Eine Kachel konnte nicht gespeichert werden ({ExceptionType}).", ex.GetType().Name);
        }
    }

    private void Prune()
    {
        try
        {
            var root = _cacheDirectory();
            if (!Directory.Exists(root))
            {
                return;
            }

            var directory = new DirectoryInfo(root);
            foreach (var leftover in directory.EnumerateFiles("*.tmp", SearchOption.AllDirectories).Where(file => file.LastWriteTimeUtc < DateTime.UtcNow.AddHours(-1)))
            {
                leftover.Delete();
            }

            var files = directory.EnumerateFiles("*.png", SearchOption.AllDirectories).OrderBy(file => file.LastWriteTimeUtc).ToList();
            var total = files.Sum(file => file.Length);
            foreach (var file in files)
            {
                if (total <= _options.MaxCacheBytes)
                {
                    break;
                }

                total -= file.Length;
                file.Delete();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug("Der Kachelspeicher konnte nicht aufgeräumt werden ({ExceptionType}).", ex.GetType().Name);
        }
    }
}
