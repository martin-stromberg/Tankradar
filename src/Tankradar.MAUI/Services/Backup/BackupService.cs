using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Umsetzung von <see cref="IBackupService"/>. Es läuft höchstens eine verändernde Operation gleichzeitig (sonst <see cref="BackupResult.Busy"/>).
/// Protokolliert werden nur Ausnahmetypen, nie Pfade oder Inhalte.
/// </summary>
public sealed class BackupService : IBackupService
{
    private static readonly string[] CountTables = ["Vehicles", "TankbookEntries", "FavoriteGroups", "FavoriteEntries", "Stations", "PriceEntries"];

    private readonly IDbContextFactory<TankradarDbContext> _contextFactory;
    private readonly IAppDataPathProvider _paths;
    private readonly IDatabaseFileProtector _protector;
    private readonly IDatabaseInitializer _initializer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BackupService> _logger;
    private readonly IPriceCacheInvalidator? _priceCache;
    private readonly Action<string>? _swapHook;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="contextFactory">Factory der Datenbankkontexte (für die Liste der Migrationen).</param>
    /// <param name="paths">Liefert das Datenverzeichnis.</param>
    /// <param name="protector">Setzt den Dateischutz auf neue Dateien.</param>
    /// <param name="initializer">Stellt vor dem Zugriff eine migrierte Datenbank sicher.</param>
    /// <param name="timeProvider">Die Zeitquelle.</param>
    /// <param name="logger">Der Logger.</param>
    /// <param name="priceCache">Verwirft nach der Wiederherstellung die Zwischenspeicher des Preisdienstes; optional.</param>
    /// <param name="swapHook">Fehlerinjektion für Tests: wird nach jedem Schritt des Austauschs aufgerufen; optional.</param>
    public BackupService(
        IDbContextFactory<TankradarDbContext> contextFactory,
        IAppDataPathProvider paths,
        IDatabaseFileProtector protector,
        IDatabaseInitializer initializer,
        TimeProvider timeProvider,
        ILogger<BackupService> logger,
        IPriceCacheInvalidator? priceCache = null,
        Action<string>? swapHook = null)
    {
        _contextFactory = contextFactory;
        _paths = paths;
        _protector = protector;
        _initializer = initializer;
        _timeProvider = timeProvider;
        _logger = logger;
        _priceCache = priceCache;
        _swapHook = swapHook;
    }

    /// <inheritdoc />
    public async Task<BackupOutcome> CreateAsync(CancellationToken cancellationToken = default)
    {
        if (!_gate.Wait(0, CancellationToken.None))
        {
            return new BackupOutcome(BackupResult.Busy);
        }

        string? workDirectory = null;
        string? tempPath = null;
        try
        {
            await _initializer.InitializeAsync().ConfigureAwait(false);
            var dataDirectory = _paths.GetDataDirectory();
            var directory = GetBackupDirectory();
            Directory.CreateDirectory(directory);
            workDirectory = Path.Combine(directory, BackupFormat.WorkDirectoryPrefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workDirectory);

            var snapshotPath = Path.Combine(workDirectory, TankradarDbContext.DatabaseFileName);
            var header = CreateSnapshot(TankradarDbContext.GetDatabasePath(dataDirectory), snapshotPath);
            header.AppVersion = AppIdentity.GetAppVersion();
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            header.CreatedUtc = now;

            var finalPath = GetUniquePath(directory, now);
            tempPath = finalPath + BackupFormat.TempExtension;
            await BackupArchiveWriter.WriteAsync(tempPath, snapshotPath, Path.Combine(dataDirectory, ReceiptPhotoStore.DirectoryName), header, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, finalPath);
            tempPath = null;
            _protector.ProtectFile(finalPath);
            return new BackupOutcome(BackupResult.Ok, ReadInfo(finalPath));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Sicherung konnte nicht erstellt werden ({ExceptionType}).", ex.GetType().Name);
            return new BackupOutcome(BackupResult.Failed);
        }
        finally
        {
            BackupFileCleanup.DeleteFile(tempPath);
            BackupFileCleanup.DeleteDirectory(workDirectory);
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BackupFileInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = GetBackupDirectory();
            if (!Directory.Exists(directory))
            {
                return [];
            }

            CleanUpLeftovers(directory);
            var list = new List<BackupFileInfo>();
            foreach (var path in Directory.EnumerateFiles(directory))
            {
                if (!BackupFormat.IsValidFileName(Path.GetFileName(path)))
                {
                    continue;
                }

                try
                {
                    list.Add(ReadInfo(path));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Eine Datei, die gerade verschwindet oder gesperrt ist, darf die Liste nicht verhindern.
                    _logger.LogWarning("Eine Sicherung konnte nicht gelesen werden ({ExceptionType}).", ex.GetType().Name);
                }
            }

            return list
                .OrderByDescending(info => info.CreatedUtc)
                .ThenByDescending(info => info.FileName, StringComparer.Ordinal)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<BackupOutcome> InspectAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var path = GetFullPath(fileName);
        if (path is null)
        {
            return new BackupOutcome(BackupResult.NotFound);
        }

        try
        {
            var known = await GetKnownMigrationsAsync().ConfigureAwait(false);
            var outcome = BackupArchiveValidator.Validate(path, known);
            return outcome.Result is BackupResult.Ok or BackupResult.TooNew
                ? outcome with { File = ReadInfo(path) }
                : outcome;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning("Die Sicherung konnte nicht geprüft werden ({ExceptionType}).", ex.GetType().Name);
            return new BackupOutcome(BackupResult.Failed);
        }
    }

    /// <inheritdoc />
    public async Task<BackupResult> RestoreAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (!_gate.Wait(0, CancellationToken.None))
        {
            return BackupResult.Busy;
        }

        string? staging = null;
        try
        {
            await _initializer.InitializeAsync().ConfigureAwait(false);
            var path = GetFullPath(fileName);
            if (path is null)
            {
                return BackupResult.NotFound;
            }

            var known = await GetKnownMigrationsAsync().ConfigureAwait(false);
            var validation = BackupArchiveValidator.Validate(path, known);
            if (validation.Result != BackupResult.Ok || validation.Manifest is null)
            {
                return validation.Result;
            }

            var dataDirectory = _paths.GetDataDirectory();
            staging = Path.Combine(dataDirectory, BackupFormat.StagingDirectoryName, Guid.NewGuid().ToString("N"));
            var extracted = await BackupArchiveValidator.ExtractAsync(path, validation.Manifest, staging, cancellationToken).ConfigureAwait(false);
            if (extracted != BackupResult.Ok)
            {
                return extracted;
            }

            var prepared = await PrepareDatabaseAsync(Path.Combine(staging, TankradarDbContext.DatabaseFileName), validation.Manifest, cancellationToken).ConfigureAwait(false);
            if (prepared != BackupResult.Ok)
            {
                return prepared;
            }

            new RestoreSwap(dataDirectory, _protector, _swapHook).Execute(staging);
            _priceCache?.InvalidateCaches();
            return BackupResult.Ok;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Wiederherstellung ist fehlgeschlagen ({ExceptionType}).", ex.GetType().Name);
            return BackupResult.Failed;
        }
        finally
        {
            BackupFileCleanup.DeleteDirectory(staging);
            BackupFileCleanup.DeleteDirectory(Path.Combine(_paths.GetDataDirectory(), BackupFormat.StagingDirectoryName), onlyIfEmpty: true);
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<BackupOutcome> ImportAsync(PickedBackupFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (!_gate.Wait(0, CancellationToken.None))
        {
            return new BackupOutcome(BackupResult.Busy);
        }

        string? tempPath = null;
        try
        {
            var directory = GetBackupDirectory();
            Directory.CreateDirectory(directory);
            tempPath = Path.Combine(directory, "import-" + Guid.NewGuid().ToString("N") + BackupFormat.TempExtension);
            if (!await CopyLimitedAsync(file, tempPath, cancellationToken).ConfigureAwait(false))
            {
                return new BackupOutcome(BackupResult.InvalidFile);
            }

            var known = await GetKnownMigrationsAsync().ConfigureAwait(false);
            var validation = BackupArchiveValidator.Validate(tempPath, known);
            if (validation.Result != BackupResult.Ok || validation.Manifest is null)
            {
                return validation;
            }

            if (await BackupArchiveValidator.VerifyContentAsync(tempPath, validation.Manifest, cancellationToken).ConfigureAwait(false) != BackupResult.Ok)
            {
                return new BackupOutcome(BackupResult.InvalidFile);
            }

            var finalPath = GetUniquePath(directory, DateTime.SpecifyKind(validation.Manifest.CreatedUtc, DateTimeKind.Utc));
            File.Move(tempPath, finalPath);
            tempPath = null;
            _protector.ProtectFile(finalPath);
            return new BackupOutcome(BackupResult.Ok, ReadInfo(finalPath));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Sicherung konnte nicht eingespielt werden ({ExceptionType}).", ex.GetType().Name);
            return new BackupOutcome(BackupResult.Failed);
        }
        finally
        {
            BackupFileCleanup.DeleteFile(tempPath);
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task<BackupResult> DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (!_gate.Wait(0, CancellationToken.None))
        {
            return Task.FromResult(BackupResult.Busy);
        }

        try
        {
            var path = GetFullPath(fileName);
            if (path is null)
            {
                return Task.FromResult(BackupResult.NotFound);
            }

            File.Delete(path);
            return Task.FromResult(BackupResult.Ok);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning("Die Sicherung konnte nicht gelöscht werden ({ExceptionType}).", ex.GetType().Name);
            return Task.FromResult(BackupResult.Failed);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public string? GetFullPath(string fileName)
    {
        if (!BackupFormat.IsValidFileName(fileName))
        {
            return null;
        }

        var path = Path.Combine(GetBackupDirectory(), fileName);
        return File.Exists(path) ? path : null;
    }

    private string GetBackupDirectory()
    {
        return Path.Combine(_paths.GetDataDirectory(), BackupFormat.BackupDirectoryName);
    }

    private async Task<IReadOnlyList<string>> GetKnownMigrationsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return context.Database.GetMigrations().ToList();
    }

    private static string GetUniquePath(string directory, DateTime createdUtc)
    {
        var stamp = createdUtc.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var candidate = Path.Combine(directory, BackupFormat.FileNamePrefix + stamp + BackupFormat.FileExtension);
        for (var counter = 2; File.Exists(candidate); counter++)
        {
            candidate = Path.Combine(directory, string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{BackupFormat.FileNamePrefix}{stamp}-{counter}{BackupFormat.FileExtension}"));
        }

        return candidate;
    }

    private static BackupFileInfo ReadInfo(string path)
    {
        var info = new FileInfo(path);
        var manifest = BackupArchiveValidator.ReadManifest(path);
        if (manifest is { Format: BackupFormat.FormatName, SchemaVersion: >= 1 })
        {
            return new BackupFileInfo(info.Name, DateTime.SpecifyKind(manifest.CreatedUtc, DateTimeKind.Utc), info.Length, manifest.AppVersion ?? string.Empty, true);
        }

        return new BackupFileInfo(info.Name, info.LastWriteTimeUtc, info.Length, string.Empty, false);
    }

    private static void CleanUpLeftovers(string directory)
    {
        try
        {
            foreach (var path in Directory.EnumerateFiles(directory, "*" + BackupFormat.TempExtension))
            {
                BackupFileCleanup.DeleteFile(path);
            }

            foreach (var path in Directory.EnumerateDirectories(directory, BackupFormat.WorkDirectoryPrefix + "*"))
            {
                BackupFileCleanup.DeleteDirectory(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Aufräumen ist nicht kritisch.
        }
    }

    private static HeaderInfo ReadHeaderInfo(SqliteConnection connection)
    {
        var header = new HeaderInfo();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1";
            header.Migration = command.ExecuteScalar() as string ?? string.Empty;
        }

        var counts = new int[CountTables.Length];
        for (var index = 0; index < CountTables.Length; index++)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM \"" + CountTables[index] + "\"";
            counts[index] = Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }

        header.Counts = new BackupManifestCounts
        {
            Vehicles = counts[0],
            Entries = counts[1],
            Groups = counts[2],
            Favorites = counts[3],
            Stations = counts[4],
            Prices = counts[5],
        };
        return header;
    }

    private static BackupManifest CreateSnapshot(string databasePath, string snapshotPath)
    {
        var sourceConnection = TankradarDbContext.BuildConnectionString(databasePath, pooling: false);
        var targetConnection = new SqliteConnectionStringBuilder { DataSource = snapshotPath, Pooling = false }.ToString();
        using var source = new SqliteConnection(sourceConnection);
        using var target = new SqliteConnection(targetConnection);
        source.Open();
        target.Open();
        source.BackupDatabase(target);
        using (var command = target.CreateCommand())
        {
            command.CommandText = "PRAGMA journal_mode=DELETE";
            command.ExecuteNonQuery();
        }

        var info = ReadHeaderInfo(target);
        return new BackupManifest { DatabaseMigration = info.Migration, Counts = info.Counts };
    }

    private async Task<BackupResult> PrepareDatabaseAsync(string databasePath, BackupManifest manifest, CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString();
            await using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                if (!await IsIntegrityOkAsync(connection, cancellationToken).ConfigureAwait(false))
                {
                    return BackupResult.InvalidFile;
                }

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1";
                var latest = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
                if (!string.Equals(latest, manifest.DatabaseMigration, StringComparison.Ordinal))
                {
                    return BackupResult.InvalidFile;
                }
            }

            var options = new DbContextOptionsBuilder<TankradarDbContext>().UseSqlite(TankradarDbContext.BuildConnectionString(databasePath, pooling: false)).Options;
            await using (var context = new TankradarDbContext(options))
            {
                await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using var check = connection.CreateCommand();
                check.CommandText = "PRAGMA foreign_key_check";
                await using (var reader = await check.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        return BackupResult.InvalidFile;
                    }
                }

                await using var mode = connection.CreateCommand();
                mode.CommandText = "PRAGMA journal_mode=DELETE";
                await mode.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            BackupFileCleanup.DeleteFile(databasePath + "-wal");
            BackupFileCleanup.DeleteFile(databasePath + "-shm");
            return BackupResult.Ok;
        }
        catch (SqliteException)
        {
            return BackupResult.InvalidFile;
        }
    }

    private static async Task<bool> IsIntegrityOkAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check";
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        return string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> CopyLimitedAsync(PickedBackupFile file, string targetPath, CancellationToken cancellationToken)
    {
        await using var source = await file.OpenReadAsync().ConfigureAwait(false);
        await using var target = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BackupLimits.BufferSize, useAsync: true);
        var buffer = new byte[BackupLimits.BufferSize];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > BackupLimits.MaxTotalBytes + BackupLimits.MaxManifestBytes)
            {
                return false;
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        return total > 0;
    }

    private sealed class HeaderInfo
    {
        public string Migration { get; set; } = string.Empty;

        public BackupManifestCounts Counts { get; set; } = new();
    }
}
