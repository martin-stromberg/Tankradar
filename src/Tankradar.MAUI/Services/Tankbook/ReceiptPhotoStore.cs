using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Umsetzung von <see cref="IReceiptPhotoStore"/> im Verzeichnis <c>&lt;Datenverzeichnis&gt;/receipts</c>. Fotos werden nicht neu kodiert;
/// jede neue Datei erhält den Dateischutz über <see cref="IDatabaseFileProtector.ProtectFile"/>. Protokolliert werden nur Ausnahmetypen.
/// </summary>
public sealed class ReceiptPhotoStore : IReceiptPhotoStore
{
    /// <summary>
    /// Name des Unterverzeichnisses der Belegfotos im Datenverzeichnis.
    /// </summary>
    public const string DirectoryName = "receipts";

    private const int BufferSize = 81_920;

    private readonly IAppDataPathProvider _paths;
    private readonly IDatabaseFileProtector _protector;
    private readonly ILogger<ReceiptPhotoStore> _logger;

    /// <summary>
    /// Erstellt die Ablage.
    /// </summary>
    /// <param name="paths">Liefert das Datenverzeichnis.</param>
    /// <param name="protector">Setzt den Dateischutz auf neue Fotos.</param>
    /// <param name="logger">Der Logger.</param>
    public ReceiptPhotoStore(IAppDataPathProvider paths, IDatabaseFileProtector protector, ILogger<ReceiptPhotoStore> logger)
    {
        _paths = paths;
        _protector = protector;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReceiptPhotoImportResult> ImportAsync(Stream source, string extension, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var normalized = NormalizeExtension(extension);
        if (normalized is null)
        {
            return new ReceiptPhotoImportResult(null, ReceiptPhotoError.UnsupportedFormat);
        }

        var fileName = Guid.NewGuid().ToString("N") + normalized;
        var directory = GetDirectory();
        var path = Path.Combine(directory, fileName);
        try
        {
            Directory.CreateDirectory(directory);
            long written = 0;
            await using (var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
            {
                var buffer = new byte[BufferSize];
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    written += read;
                    if (written > TankbookEntryLimits.MaxReceiptPhotoBytes)
                    {
                        break;
                    }

                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
            }

            if (written > TankbookEntryLimits.MaxReceiptPhotoBytes)
            {
                TryDeleteFile(path);
                return new ReceiptPhotoImportResult(null, ReceiptPhotoError.TooLarge);
            }

            if (written == 0)
            {
                TryDeleteFile(path);
                return new ReceiptPhotoImportResult(null, ReceiptPhotoError.UnsupportedFormat);
            }

            _protector.ProtectFile(path);
            return new ReceiptPhotoImportResult(fileName, ReceiptPhotoError.None);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Das Belegfoto konnte nicht übernommen werden ({ExceptionType}).", ex.GetType().Name);
            TryDeleteFile(path);
            return new ReceiptPhotoImportResult(null, ReceiptPhotoError.Failed);
        }
        catch (OperationCanceledException)
        {
            TryDeleteFile(path);
            throw;
        }
    }

    /// <inheritdoc />
    public string? GetFullPath(string fileName)
    {
        return IsValidFileName(fileName) ? Path.Combine(GetDirectory(), fileName) : null;
    }

    /// <inheritdoc />
    public bool Exists(string fileName)
    {
        return GetFullPath(fileName) is { } path && File.Exists(path);
    }

    /// <inheritdoc />
    public void Delete(string fileName)
    {
        if (GetFullPath(fileName) is { } path)
        {
            TryDeleteFile(path);
        }
    }

    /// <inheritdoc />
    public void DeleteUnreferenced(IReadOnlySet<string> referencedFileNames)
    {
        ArgumentNullException.ThrowIfNull(referencedFileNames);
        var directory = GetDirectory();
        if (!Directory.Exists(directory))
        {
            return;
        }

        try
        {
            foreach (var path in Directory.EnumerateFiles(directory))
            {
                var fileName = Path.GetFileName(path);
                if (IsValidFileName(fileName) && !referencedFileNames.Contains(fileName))
                {
                    TryDeleteFile(path);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning("Nicht mehr verwendete Belegfotos konnten nicht bereinigt werden ({ExceptionType}).", ex.GetType().Name);
        }
    }

    /// <summary>
    /// Prüft, ob ein Dateiname dem Format der Ablage entspricht (<c>&lt;Guid ohne Bindestriche&gt;.&lt;erlaubte Endung&gt;</c>); schützt vor Pfadangaben.
    /// </summary>
    /// <param name="fileName">Der Dateiname.</param>
    /// <returns><see langword="true"/>, wenn der Name gültig ist.</returns>
    public static bool IsValidFileName(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        var dot = fileName.IndexOf('.', StringComparison.Ordinal);
        if (dot != 32)
        {
            return false;
        }

        var extension = fileName[dot..];
        return Guid.TryParseExact(fileName[..dot], "N", out _)
            && TankbookEntryLimits.ReceiptPhotoExtensions.Contains(extension, StringComparer.Ordinal);
    }

    private static string? NormalizeExtension(string? extension)
    {
        var trimmed = extension?.Trim().ToLowerInvariant() ?? string.Empty;
        if (trimmed.Length > 0 && trimmed[0] != '.')
        {
            trimmed = "." + trimmed;
        }

        return TankbookEntryLimits.ReceiptPhotoExtensions.Contains(trimmed, StringComparer.Ordinal) ? trimmed : null;
    }

    private string GetDirectory()
    {
        return Path.Combine(_paths.GetDataDirectory(), DirectoryName);
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning("Ein Belegfoto konnte nicht gelöscht werden ({ExceptionType}).", ex.GetType().Name);
        }
    }
}
