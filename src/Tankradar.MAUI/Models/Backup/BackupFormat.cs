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
    /// Magic-Bytes am Anfang einer verschlüsselten Sicherung (<c>TKBENC01</c>, bewusst ohne <c>PK</c>-Anfang, ADR 0019).
    /// </summary>
    public static ReadOnlySpan<byte> EncryptedMagic => "TKBENC01"u8;

    /// <summary>
    /// Aktuelle Version des verschlüsselten Containers (steht im unverschlüsselten Header, ADR 0019).
    /// </summary>
    public const ushort EncryptedContainerVersion = 1;

    /// <summary>
    /// KDF-Kennung des verschlüsselten Containers: PBKDF2-HMAC-SHA256.
    /// </summary>
    public const ushort EncryptedKdfPbkdf2Sha256 = 1;

    /// <summary>
    /// Iterationszahl der Schlüsselableitung (PBKDF2-HMAC-SHA256, OWASP-Richtwert); steht zusätzlich im Header jeder Datei.
    /// </summary>
    public const int Pbkdf2Iterations = 600_000;

    /// <summary>
    /// Klartext-Größe eines Verschlüsselungschunks in Bytes (1 MiB).
    /// </summary>
    public const int EncryptedChunkSize = 1024 * 1024;

    /// <summary>
    /// Länge des abgeleiteten Schlüssels in Bytes (AES-256).
    /// </summary>
    public const int EncryptedKeyBytes = 32;

    /// <summary>
    /// Länge des Salts der Schlüsselableitung in Bytes.
    /// </summary>
    public const int EncryptedSaltBytes = 16;

    /// <summary>
    /// Länge der Nonce-Basis und der Nonce je Chunk in Bytes (AES-GCM).
    /// </summary>
    public const int EncryptedNonceBytes = 12;

    /// <summary>
    /// Länge des Authentifikations-Tags je Chunk in Bytes.
    /// </summary>
    public const int EncryptedTagBytes = 16;

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
