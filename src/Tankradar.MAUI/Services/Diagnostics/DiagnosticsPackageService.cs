using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Tankradar.MAUI.Services.Diagnostics;

/// <summary>
/// Baut das Diagnosepaket: Metadaten, redigierter Protokollauszug und eine Inhaltsbeschreibung als lokale ZIP-Datei.
/// Es werden ausschließlich Daten aufgenommen, die ohne personenbezogene Inhalte auskommen.
/// </summary>
public sealed class DiagnosticsPackageService : IDiagnosticsPackageService
{
    /// <summary>
    /// Höchstzahl der Protokolleinträge im Paket.
    /// </summary>
    public const int MaxEntries = 200;

    /// <summary>
    /// Höchstlänge einer einzelnen Protokollzeile im Paket.
    /// </summary>
    public const int MaxEntryChars = 500;

    /// <summary>
    /// Gesamtgrößenbegrenzung des Protokollausschnitts.
    /// </summary>
    public const int MaxLogBytes = 128 * 1024;

    /// <summary>
    /// Dateinamenspräfix der Diagnosepakete.
    /// </summary>
    public const string FileNamePrefix = "tankatlas-diagnose-";

    /// <summary>
    /// Name des Unterordners im Zwischenspeicher.
    /// </summary>
    public const string DirectoryName = "diagnostics";

    internal const string ManifestEntryName = "paket-info.json";
    internal const string LogEntryName = "protokoll.txt";
    internal const string ReadmeEntryName = "liesmich.txt";

    private readonly IDiagnosticsLogBuffer _buffer;
    private readonly ISensitiveValueProvider _sensitiveValues;
    private readonly TimeProvider _timeProvider;
    private readonly Func<DiagnosticsEnvironmentInfo> _environmentInfo;
    private readonly string _directory;
    private readonly ILogger<DiagnosticsPackageService> _logger;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="buffer">Der Protokollpuffer.</param>
    /// <param name="sensitiveValues">Die Quelle wörtlich zu redigierender Geheimnisse.</param>
    /// <param name="timeProvider">Die Zeitquelle.</param>
    /// <param name="environmentInfo">Liefert die unverfänglichen Rahmendaten.</param>
    /// <param name="directory">Das Ablageverzeichnis der Pakete.</param>
    /// <param name="logger">Logger für interne Hinweise (selbst gepuffert, nie ins Paket ohne Redaktion).</param>
    public DiagnosticsPackageService(
        IDiagnosticsLogBuffer buffer,
        ISensitiveValueProvider sensitiveValues,
        TimeProvider timeProvider,
        Func<DiagnosticsEnvironmentInfo> environmentInfo,
        string directory,
        ILogger<DiagnosticsPackageService> logger)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(sensitiveValues);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(environmentInfo);
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentNullException.ThrowIfNull(logger);
        _buffer = buffer;
        _sensitiveValues = sensitiveValues;
        _timeProvider = timeProvider;
        _environmentInfo = environmentInfo;
        _directory = directory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> CreatePreviewTextAsync(CancellationToken cancellationToken = default)
    {
        var content = await BuildContentAsync(cancellationToken).ConfigureAwait(false);
        var text = new StringBuilder(content.Readme.Length + content.LogExcerpt.Length + 256);
        text.AppendLine(content.Readme);
        text.AppendLine();
        text.AppendLine(content.ManifestJson);
        text.AppendLine();
        text.Append(content.LogExcerpt);
        return text.ToString();
    }

    /// <inheritdoc />
    public async Task<string> CreatePackageAsync(CancellationToken cancellationToken = default)
    {
        var content = await BuildContentAsync(cancellationToken).ConfigureAwait(false);

        Directory.CreateDirectory(_directory);
        foreach (var existing in Directory.GetFiles(_directory, FileNamePrefix + "*.zip"))
        {
            try
            {
                File.Delete(existing);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Ein älteres Diagnosepaket konnte nicht entfernt werden ({ExceptionType}).", ex.GetType().Name);
            }
        }

        var fileName = FileNamePrefix + _timeProvider.GetUtcNow().ToString("yyyyMMdd-HHmmss") + ".zip";
        var fullPath = Path.Combine(_directory, fileName);
        using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            WriteEntry(archive, ManifestEntryName, content.ManifestJson);
            WriteEntry(archive, LogEntryName, content.LogExcerpt);
            WriteEntry(archive, ReadmeEntryName, content.Readme);
        }

        return fullPath;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private async Task<DiagnosticsPackageContent> BuildContentAsync(CancellationToken cancellationToken)
    {
        var sensitive = await _sensitiveValues.GetValuesAsync(cancellationToken).ConfigureAwait(false);
        var info = _environmentInfo();
        var now = _timeProvider.GetUtcNow();

        var manifest = new StringBuilder(512);
        manifest.AppendLine("{");
        manifest.AppendLine("  \"schemaVersion\": 1,");
        manifest.AppendLine($"  \"erstelltAmUtc\": \"{now:O}\",");
        manifest.AppendLine($"  \"appVersion\": \"{info.AppVersion}\",");
        manifest.AppendLine($"  \"betriebssystem\": \"{info.OperatingSystem}\",");
        manifest.AppendLine($"  \"geraet\": \"{info.Device}\",");
        manifest.AppendLine($"  \"laufzeit\": \"{info.Runtime}\"");
        manifest.AppendLine("}");

        var log = new StringBuilder(MaxLogBytes);
        var entries = _buffer.Snapshot();
        var skipped = 0;
        var written = 0;
        foreach (var entry in entries)
        {
            if (written >= MaxEntries || log.Length >= MaxLogBytes)
            {
                skipped++;
                continue;
            }

            var message = DiagnosticsRedactor.Redact(entry.Message, sensitive);
            var line = string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{entry.Timestamp:yyyy-MM-ddTHH:mm:ssZ} {entry.Level,-11} {entry.Category}: {message}");
            if (entry.ExceptionType is not null)
            {
                line += " (" + entry.ExceptionType + ")";
            }

            if (line.Length > MaxEntryChars)
            {
                line = line[..MaxEntryChars] + "…";
            }

            log.AppendLine(line);
            written++;
        }

        if (written == 0)
        {
            log.AppendLine("(Keine Protokolleinträge seit dem letzten App-Start.)");
        }
        else if (skipped > 0)
        {
            log.AppendLine($"(… {skipped} ältere Einträge wegen der Größenbegrenzung weggelassen.)");
        }

        var readme =
            "Tankatlas-Diagnosepaket\r\n" +
            "========================\r\n\r\n" +
            "Inhalt:\r\n" +
            "- paket-info.json: App-Version, Betriebssystem, Gerätemodell, Laufzeitversion, Erstellzeitpunkt.\r\n" +
            "- protokoll.txt: Warnungen und Fehler seit dem letzten App-Start (höchstens die neuesten Einträge).\r\n\r\n" +
            "Nicht enthalten sind grundsätzlich: API-Schlüssel, Standortdaten, Tankstellen- oder Stationsnamen,\r\n" +
            "Suchbegriffe, Dateipfade, Belegfotos, Tankbuch-Einträge und Einstellungen. Vertraulich wirkende\r\n" +
            "Zeichenfolgen wurden zusätzlich durch " + DiagnosticsRedactor.Placeholder + " ersetzt.\r\n";

        return new DiagnosticsPackageContent(manifest.ToString(), log.ToString(), readme);
    }

    private sealed record DiagnosticsPackageContent(string ManifestJson, string LogExcerpt, string Readme);
}
