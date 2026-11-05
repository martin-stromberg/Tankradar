using System.Text.RegularExpressions;

namespace Tankradar.MAUI.Models.Backup;

/// <summary>
/// Kennungen und Namen des Sicherungsformats (ZIP mit Manifest, Datenbank und Belegfotos).
/// </summary>
public static partial class BackupFormat
{
    /// <summary>
    /// Wert des Manifestfelds <c>format</c>.
    /// </summary>
    public const string FormatName = "Tankatlas-Backup";

    /// <summary>
    /// Aktuelle Version des Sicherungsformats (nicht der Datenbank).
    /// </summary>
    public const int SchemaVersion = 1;

    /// <summary>
    /// Name des Manifests im Archiv.
    /// </summary>
    public const string ManifestEntryName = "manifest.json";

    /// <summary>
    /// Name der Datenbank im Archiv.
    /// </summary>
    public const string DatabaseEntryName = "database/tankatlas.db";

    /// <summary>
    /// Präfix der Belegfotos im Archiv.
    /// </summary>
    public const string ReceiptEntryPrefix = "receipts/";

    /// <summary>
    /// Name des Unterverzeichnisses der Sicherungen im Datenverzeichnis.
    /// </summary>
    public const string BackupDirectoryName = "backups";

    /// <summary>
    /// Name des Verzeichnisses, in das eine Wiederherstellung entpackt.
    /// </summary>
    public const string StagingDirectoryName = "restore-staging";

    /// <summary>
    /// Name des Verzeichnisses, in dem der Austausch den bisherigen Stand zwischenlagert.
    /// </summary>
    public const string RollbackDirectoryName = "restore-rollback";

    /// <summary>
    /// Name der Journaldatei, die einen laufenden Austausch markiert.
    /// </summary>
    public const string JournalFileName = "restore.journal";

    /// <summary>
    /// Name des Verzeichnisses, in das der Testmodus „Teilen“ umleitet.
    /// </summary>
    public const string TestSharedDirectoryName = "test-shared";

    /// <summary>
    /// Anfang des Dateinamens einer Sicherung.
    /// </summary>
    public const string FileNamePrefix = "tankatlas-backup-";

    /// <summary>
    /// Endung einer Sicherung.
    /// </summary>
    public const string FileExtension = ".zip";

    /// <summary>
    /// Endung unfertiger Dateien.
    /// </summary>
    public const string TempExtension = ".tmp";

    /// <summary>
    /// Anfang der Namen temporärer Arbeitsverzeichnisse.
    /// </summary>
    public const string WorkDirectoryPrefix = ".work-";

    /// <summary>
    /// Prüft, ob ein Name eine Sicherungsdatei bezeichnet (<c>tankatlas-backup-*.zip</c>, ohne Pfadanteile).
    /// </summary>
    /// <param name="fileName">Der Name.</param>
    /// <returns><see langword="true"/>, wenn der Name zulässig ist.</returns>
    public static bool IsValidFileName(string? fileName)
    {
        return !string.IsNullOrEmpty(fileName)
            && fileName.Length <= 100
            && FileNameRegex().IsMatch(fileName)
            && Path.GetFileName(fileName) == fileName;
    }

    [GeneratedRegex(@"^tankatlas-backup-[0-9A-Za-z_-]+(\.[0-9A-Za-z_-]+)*\.zip$", RegexOptions.CultureInvariant)]
    private static partial Regex FileNameRegex();
}
