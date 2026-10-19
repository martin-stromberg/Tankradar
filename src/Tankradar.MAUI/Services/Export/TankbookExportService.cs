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

    private readonly IVehicleService _vehicleService;
    private readonly ITankbookEntryService _entryService;
    private readonly IAppDataPathProvider _paths;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TankbookExportService> _logger;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="vehicleService">Der Fahrzeugdienst.</param>
    /// <param name="entryService">Der Dienst der Vorgänge.</param>
    /// <param name="paths">Liefert den Zwischenspeicher.</param>
    /// <param name="timeProvider">Die Zeitquelle.</param>
    /// <param name="logger">Logger (protokolliert nie Namen oder Werte).</param>
    public TankbookExportService(
        IVehicleService vehicleService,
        ITankbookEntryService entryService,
        IAppDataPathProvider paths,
        TimeProvider timeProvider,
        ILogger<TankbookExportService> logger)
    {
        _vehicleService = vehicleService;
        _entryService = entryService;
        _paths = paths;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TankbookExportResult> CreateAsync(long vehicleId, TankbookExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = _timeProvider.GetLocalNow().DateTime;
        if (!IsValidRange(request, now))
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
                TankbookExportFormat.Pdf => new TankbookPdfReportWriter().Write(data),
                _ => throw new ArgumentOutOfRangeException(nameof(request), request.Format, "Unbekanntes Format."),
            };
            var directory = PrepareDirectory();
            var fileName = TankbookExportFileNamer.Create(vehicle.Name, now, TankbookExportTexts.GetFileExtension(request.Format));
            path = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
            return new TankbookExportResult(TankbookExportStatus.Exported, path, fileName, data.Entries.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Der Export konnte nicht erstellt werden ({ExceptionType}).", ex.GetType().Name);
            if (path is not null)
            {
                Discard(path);
            }

            return TankbookExportResult.WithoutFile(TankbookExportStatus.Failed);
        }
    }

    /// <inheritdoc />
    public void Discard(string path)
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

    private static bool IsValidRange(TankbookExportRequest request, DateTime now)
    {
        if (request.From is { } from && request.To is { } to && from.Date > to.Date)
        {
            return false;
        }

        return request.To is not { } end || end.Date <= now.Date;
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
