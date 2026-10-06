using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Map;

namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Ruft Kartenkacheln per HTTP ab und hält sie im Arbeitsspeicher und auf dem Gerät vor. Die Klasse hält die Nutzungsrichtlinie der OpenStreetMap-Kachelserver ein:
/// identifizierende Kennung, höchstens zwei parallele Abrufe, keine Wiederholung nach Fehlern innerhalb des Wartefensters, keine Weiterleitungen und keine Vorabrufe.
/// Die Gültigkeit einer Kachel richtet sich nach den HTTP-Caching-Angaben des Servers (<c>Cache-Control</c>, <c>Expires</c>); fehlen sie, gelten sieben Tage.
/// Abgelaufene Kacheln werden bedingt (<c>If-None-Match</c> bzw. <c>If-Modified-Since</c>) neu angefragt; ein <c>304</c> verlängert die Gültigkeit ohne erneute Übertragung.
/// Koordinaten oder Kachelnummern werden nie protokolliert.
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
    private readonly Dictionary<TileKey, MemoryEntry> _memory = [];
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

    private enum DownloadKind
    {
        Failed,
        Downloaded,
        NotModified,
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetTileAsync(TileKey key, CancellationToken cancellationToken = default)
    {
        if (_options.EndpointNotConfigured || !IsValid(key))
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        if (TryGetFreshMemory(key, now, out var cached))
        {
            return cached;
        }

        var stored = ReadFromDisk(key, now);
        if (stored is { IsFresh: true })
        {
            Remember(key, stored.Data, stored.ExpiresUtc);
            return stored.Data;
        }

        if (IsInBackoff(key, now))
        {
            return stored?.Data;
        }

        var outcome = await DownloadAsync(key, stored, now, cancellationToken).ConfigureAwait(false);
        switch (outcome.Kind)
        {
            case DownloadKind.Downloaded:
                Remember(key, outcome.Data!, outcome.Freshness.ExpiresUtc);
                if (!outcome.Freshness.NoStore)
                {
                    WriteToDisk(key, outcome.Data!, outcome.Metadata!);
                }

                return outcome.Data;
            case DownloadKind.NotModified when stored is not null:
                // Der Server bestätigt die gespeicherte Kachel: nur die Gültigkeit wird verlängert.
                Remember(key, stored.Data, outcome.Freshness.ExpiresUtc);
                if (!outcome.Freshness.NoStore)
                {
                    var confirmed = outcome.Metadata!;
                    WriteMetadata(key, confirmed with
                    {
                        ETag = confirmed.ETag ?? stored.Metadata?.ETag,
                        LastModified = confirmed.LastModified ?? stored.Metadata?.LastModified,
                    });
                }

                return stored.Data;
            default:
                RememberFailure(key, now);
                return stored?.Data;
        }
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

    private static void AddValidators(HttpRequestMessage request, TileMetadata? metadata)
    {
        if (metadata is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(metadata.ETag) && EntityTagHeaderValue.TryParse(metadata.ETag, out var etag))
        {
            request.Headers.IfNoneMatch.Add(etag);
        }

        if (metadata.LastModified is { } lastModified)
        {
            request.Headers.IfModifiedSince = lastModified;
        }
    }

    private static TileMetadata CreateMetadata(HttpResponseMessage response, TileFreshness freshness)
    {
        return new TileMetadata(response.Headers.ETag?.ToString(), response.Content.Headers.LastModified, freshness.ExpiresUtc);
    }

    private async Task<DownloadOutcome> DownloadAsync(TileKey key, StoredTile? stored, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await _slots.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_options.RequestTimeout);
            using var request = new HttpRequestMessage(HttpMethod.Get, _options.CreateUri(key));
            request.Headers.UserAgent.ParseAdd(_options.UserAgent);
            AddValidators(request, stored?.Metadata);
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                if (stored is null)
                {
                    return DownloadOutcome.Failed;
                }

                var confirmedFreshness = EvaluateFreshness(response, now);
                return new DownloadOutcome(DownloadKind.NotModified, null, confirmedFreshness, CreateMetadata(response, confirmedFreshness));
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Eine Kachel konnte nicht abgerufen werden (Status {Status}).", (int)response.StatusCode);
                return DownloadOutcome.Failed;
            }

            if (response.Content.Headers.ContentLength is { } length && length > _options.MaxTileBytes)
            {
                return DownloadOutcome.Failed;
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
                    return DownloadOutcome.Failed;
                }
            }

            var data = buffer.ToArray();
            if (!LooksLikePng(data))
            {
                return DownloadOutcome.Failed;
            }

            var freshness = EvaluateFreshness(response, now);
            return new DownloadOutcome(DownloadKind.Downloaded, data, freshness, CreateMetadata(response, freshness));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Der Abruf einer Kachel hat das Zeitlimit überschritten.");
            return DownloadOutcome.Failed;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug("Eine Kachel konnte nicht abgerufen werden ({ExceptionType}).", ex.GetType().Name);
            return DownloadOutcome.Failed;
        }
        finally
        {
            _slots.Release();
        }
    }

    private TileFreshness EvaluateFreshness(HttpResponseMessage response, DateTimeOffset now)
    {
        return TileCachePolicy.Evaluate(response.Headers.CacheControl, response.Content.Headers.Expires, response.Headers.Date, now, _options.CacheLifetime);
    }

    private bool TryGetFreshMemory(TileKey key, DateTimeOffset now, out byte[]? data)
    {
        lock (_gate)
        {
            if (_memory.TryGetValue(key, out var entry) && now < entry.ExpiresUtc)
            {
                data = entry.Data;
                return true;
            }

            data = null;
            return false;
        }
    }

    private void Remember(TileKey key, byte[] data, DateTimeOffset expiresUtc)
    {
        lock (_gate)
        {
            if (!_memory.ContainsKey(key))
            {
                _memoryOrder.Enqueue(key);
            }

            _memory[key] = new MemoryEntry(data, expiresUtc);
            while (_memory.Count > MemoryCapacity && _memoryOrder.TryDequeue(out var oldest))
            {
                _memory.Remove(oldest);
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

    private string MetadataPathOf(TileKey key)
    {
        return PathOf(key) + ".meta";
    }

    private StoredTile? ReadFromDisk(TileKey key, DateTimeOffset now)
    {
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

            var metadata = ReadMetadata(key);

            // Kacheln ohne Angaben (aus einer früheren Version) gelten ab ihrer Dateizeit für die Rückfallfrist.
            var expires = metadata?.ExpiresUtc ?? new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero) + _options.CacheLifetime;
            return new StoredTile(data, metadata, expires, now < expires);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Der Kachelspeicher ist eine reine Beschleunigung; ein Lesefehler führt zum Abruf.
            return null;
        }
    }

    private TileMetadata? ReadMetadata(TileKey key)
    {
        try
        {
            var path = MetadataPathOf(key);
            return File.Exists(path) ? JsonSerializer.Deserialize<TileMetadata>(File.ReadAllText(path)) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Unlesbare Angaben: die Kachel wird wie eine ohne Angaben behandelt (Rückfallfrist, keine bedingte Anfrage).
            return null;
        }
    }

    private void WriteToDisk(TileKey key, byte[] data, TileMetadata metadata)
    {
        try
        {
            var path = PathOf(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllBytes(temp, data);
            File.Move(temp, path, overwrite: true);
            WriteMetadata(key, metadata);
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

    private void WriteMetadata(TileKey key, TileMetadata metadata)
    {
        try
        {
            var path = MetadataPathOf(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(metadata));
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug("Die Zwischenspeicher-Angaben einer Kachel konnten nicht gespeichert werden ({ExceptionType}).", ex.GetType().Name);
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
                var metadataFile = file.FullName + ".meta";
                if (File.Exists(metadataFile))
                {
                    File.Delete(metadataFile);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug("Der Kachelspeicher konnte nicht aufgeräumt werden ({ExceptionType}).", ex.GetType().Name);
        }
    }

    private sealed record MemoryEntry(byte[] Data, DateTimeOffset ExpiresUtc);

    private sealed record StoredTile(byte[] Data, TileMetadata? Metadata, DateTimeOffset ExpiresUtc, bool IsFresh);

    private sealed record DownloadOutcome(DownloadKind Kind, byte[]? Data, TileFreshness Freshness, TileMetadata? Metadata)
    {
        public static DownloadOutcome Failed { get; } = new(DownloadKind.Failed, null, default, null);
    }
}

/// <summary>
/// Zwischenspeicher-Angaben einer Kachel auf dem Gerät: Validatoren für bedingte Anfragen und der Zeitpunkt, bis zu dem die Kachel ohne Rückfrage gilt.
/// </summary>
/// <param name="ETag">Der <c>ETag</c> des Servers oder <see langword="null"/>.</param>
/// <param name="LastModified">Die Angabe <c>Last-Modified</c> des Servers oder <see langword="null"/>.</param>
/// <param name="ExpiresUtc">Der Zeitpunkt, bis zu dem die Kachel ohne Rückfrage gilt.</param>
/// <returns>Der Wert.</returns>
public sealed record TileMetadata(string? ETag, DateTimeOffset? LastModified, DateTimeOffset ExpiresUtc);
