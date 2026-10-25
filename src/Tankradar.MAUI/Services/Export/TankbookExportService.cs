using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.MAUI.Services.Export;

/// <summary>
/// Umsetzung von <see cref="ITankbookExportService"/>: lädt Fahrzeug und alle Vorgänge, filtert auf den Zeitraum, berechnet die Kennzahlen mit den vorhandenen Rechnern
/// und schreibt die Datei nach <c>&lt;Zwischenspeicher&gt;/exports</c>. Protokolliert werden nur Ausnahmetypen, nie Namen oder Werte.
/// </summary>
public sealed class TankbookExportService : ITankbookExportService
{
    /// <summary>
    /// Der Name des Unterverzeichnisses im Zwischenspeicher.
    /// </summary>
    public const string DirectoryName = "exports";

    private const int MaxFileNameAttempts = 99;

    private readonly IVehicleService _vehicleService;
    private readonly ITankbookEntryService _entryService;
    private readonly IAppDataPathProvider _paths;
    private readonly IDatabaseFileProtector _protector;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TankbookExportService> _logger;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="vehicleService">Der Fahrzeugdienst.</param>
    /// <param name="entryService">Der Dienst der Vorgänge.</param>
    /// <param name="paths">Liefert den Zwischenspeicher.</param>
    /// <param name="protector">Setzt den Dateischutz der fertigen Exportdatei (iOS: Data Protection).</param>
    /// <param name="timeProvider">Die Zeitquelle.</param>
    /// <param name="logger">Logger (protokolliert nie Namen oder Werte).</param>
    public TankbookExportService(
        IVehicleService vehicleService,
        ITankbookEntryService entryService,
        IAppDataPathProvider paths,
        IDatabaseFileProtector protector,
        TimeProvider timeProvider,
        ILogger<TankbookExportService> logger)
    {
        _vehicleService = vehicleService;
        _entryService = entryService;
        _paths = paths;
        _protector = protector;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TankbookExportResult> CreateAsync(long vehicleId, TankbookExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = _timeProvider.GetLocalNow().DateTime;
        if (TankbookExportRangeResolver.Validate(request.From, request.To, now) != TankbookExportRangeError.None)
        {
            return TankbookExportResult.WithoutFile(TankbookExportStatus.InvalidRange);
        }

        string? path = null;
        try
        {
            var vehicle = await _vehicleService.GetVehicleAsync(vehicleId, cancellationToken).ConfigureAwait(false);
            if (vehicle is null)
            {
                return TankbookExportResult.WithoutFile(TankbookExportStatus.VehicleNotFound);
            }

            var overviews = await _entryService.GetEntriesAsync(vehicleId, cancellationToken).ConfigureAwait(false);
            var data = TankbookExportDataBuilder.Build(vehicle, overviews, request.From, request.To, now);
            if (data.Entries.Count == 0)
            {
                return TankbookExportResult.WithoutFile(TankbookExportStatus.NoEntries);
            }

            var bytes = request.Format switch
            {
                TankbookExportFormat.Csv => TankbookCsvWriter.Write(data),
                TankbookExportFormat.Pdf => TankbookPdfReportWriter.Write(data),
                _ => throw new ArgumentOutOfRangeException(nameof(request), request.Format, "Unbekanntes Format."),
            };
            var directory = PrepareDirectory();
            var fileName = TankbookExportFileNamer.Create(vehicle.Name, now, TankbookExportTexts.GetFileExtension(request.Format));
            var stream = CreateUniqueFile(directory, fileName);
            path = stream.Name;
            await using (stream.ConfigureAwait(false))
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            }

            // Die Datei enthält personenbezogene Tankbuchdaten: Sie erhält wie die Datenbank und die Belegfotos den Dateischutz des Betriebssystems.
            _protector.ProtectFile(path);

            return new TankbookExportResult(TankbookExportStatus.Exported, path, Path.GetFileName(path), data.Entries.Count);
        }
        catch (OperationCanceledException)
        {
            if (path is not null)
            {
                Discard(path);
            }

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Der Export konnte nicht erstellt werden ({ExceptionType}).", ex.GetType().Name);
            if (path is not null)
            {
                Discard(path);
            }

            return TankbookExportResult.WithoutFile(TankbookExportStatus.Failed);
        }
    }

    private void Discard(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug("Die Exportdatei konnte nicht gelöscht werden ({ExceptionType}).", ex.GetType().Name);
        }
    }

    private static FileStream CreateUniqueFile(string directory, string fileName)
    {
        // Eine noch gesperrte Vorversion (unter Windows z. B. in einem Betrachter geöffnet) bleibt beim Aufräumen liegen: Dann weicht der Name mit einem Zähler aus.
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var counter = 1; counter <= MaxFileNameAttempts; counter++)
        {
            var candidate = Path.Combine(directory, counter == 1 ? fileName : $"{baseName}_{counter}{extension}");
            try
            {
                return new FileStream(candidate, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }
            catch (IOException) when (File.Exists(candidate))
            {
                // Name belegt: nächster Versuch.
            }
        }

        throw new IOException("Es gibt keinen freien Dateinamen für den Export.");
    }

    private string PrepareDirectory()
    {
        var directory = Path.Combine(_paths.GetCacheDirectory(), DirectoryName);
        Directory.CreateDirectory(directory);
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            Discard(file);
        }

        return directory;
    }
}
