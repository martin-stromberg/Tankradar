using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Tankradar.TestSupport;

/// <summary>
/// Erzeugt Sicherungsarchive für Tests (gültig, zu neu, mit Zip-Slip-Einträgen, falscher Prüfsumme, abgeschnitten), unabhängig vom Produktionscode.
/// </summary>
public static class BackupTestArchive
{
    /// <summary>
    /// Eine beliebige, nur für Namenstests gedachte Datenbankmigration.
    /// </summary>
    public const string DefaultMigration = "20261011170920_AddTankbookEntryDetails";

    /// <summary>
    /// Schreibt ein Archiv mit Manifest, Datenbank und Fotos.
    /// </summary>
    /// <param name="path">Der Zielpfad.</param>
    /// <param name="database">Der Inhalt der Datenbank.</param>
    /// <param name="photos">Die Belegfotos (Dateiname ohne Pfad zu Inhalt); darf <see langword="null"/> sein.</param>
    /// <param name="migration">Die Migration im Manifest.</param>
    /// <param name="schemaVersion">Die Formatversion im Manifest.</param>
    /// <param name="appVersion">Die App-Version im Manifest.</param>
    /// <param name="createdUtc">Der Erstellungszeitpunkt im Manifest.</param>
    /// <param name="wrongDatabaseHash">Schreibt eine falsche Prüfsumme für die Datenbank.</param>
    /// <param name="format">Die Formatkennung im Manifest.</param>
    /// <param name="extraEntries">Zusätzliche Einträge (Name, Inhalt), die weder im Manifest stehen noch geprüft werden; auch für Zip-Slip-Namen.</param>
    /// <param name="listExtraEntriesInManifest">Trägt die zusätzlichen Einträge in die Dateiliste des Manifests ein.</param>
    /// <param name="declaredDatabaseSize">Überschreibt die im Manifest deklarierte Größe der Datenbank.</param>
    public static void Create(
        string path,
        byte[] database,
        IReadOnlyDictionary<string, byte[]>? photos = null,
        string migration = DefaultMigration,
        int schemaVersion = 1,
        string appVersion = "0.1.19",
        DateTime? createdUtc = null,
        bool wrongDatabaseHash = false,
        string format = "Tankatlas-Backup",
        IReadOnlyList<(string Name, byte[] Content)>? extraEntries = null,
        bool listExtraEntriesInManifest = false,
        long? declaredDatabaseSize = null)
    {
        var files = new List<object>();
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            AddEntry(archive, "database/tankatlas.db", database);
            files.Add(new { path = "database/tankatlas.db", size = declaredDatabaseSize ?? database.Length, sha256 = wrongDatabaseHash ? new string('0', 64) : Hash(database) });
            foreach (var photo in photos ?? new Dictionary<string, byte[]>())
            {
                AddEntry(archive, "receipts/" + photo.Key, photo.Value);
                files.Add(new { path = "receipts/" + photo.Key, size = photo.Value.Length, sha256 = Hash(photo.Value) });
            }

            foreach (var (name, content) in extraEntries ?? [])
            {
                AddEntry(archive, name, content);
                if (listExtraEntriesInManifest)
                {
                    files.Add(new { path = name, size = content.Length, sha256 = Hash(content) });
                }
            }

            var manifest = new
            {
                format,
                schemaVersion,
                appVersion,
                databaseMigration = migration,
                createdUtc = createdUtc ?? new DateTime(2026, 10, 12, 11, 15, 0, DateTimeKind.Utc),
                files,
                counts = new { vehicles = 1, entries = 2, groups = 3, favorites = 4, stations = 5, prices = 6, photos = photos?.Count ?? 0 },
            };
            AddEntry(archive, "manifest.json", JsonSerializer.SerializeToUtf8Bytes(manifest));
        }
    }

    /// <summary>
    /// Schreibt ein Archiv ohne Manifest.
    /// </summary>
    /// <param name="path">Der Zielpfad.</param>
    /// <param name="database">Der Inhalt der Datenbank.</param>
    public static void CreateWithoutManifest(string path, byte[] database)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        AddEntry(archive, "database/tankatlas.db", database);
    }

    /// <summary>
    /// Schreibt ein Archiv mit frei wählbarem Inhalt des Manifests.
    /// </summary>
    /// <param name="path">Der Zielpfad.</param>
    /// <param name="database">Der Inhalt der Datenbank.</param>
    /// <param name="manifestBytes">Der Inhalt von <c>manifest.json</c>.</param>
    public static void CreateWithManifestBytes(string path, byte[] database, byte[] manifestBytes)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        AddEntry(archive, "database/tankatlas.db", database);
        AddEntry(archive, "manifest.json", manifestBytes);
    }

    /// <summary>
    /// Schneidet eine Datei auf die halbe Länge ab (beschädigtes Archiv).
    /// </summary>
    /// <param name="path">Der Pfad der Datei.</param>
    public static void Truncate(string path)
    {
        var bytes = File.ReadAllBytes(path);
        File.WriteAllBytes(path, bytes[..(bytes.Length / 2)]);
    }

    /// <summary>
    /// Berechnet die SHA-256-Prüfsumme in Kleinbuchstaben.
    /// </summary>
    /// <param name="content">Der Inhalt.</param>
    /// <returns>Die hexadezimale Prüfsumme.</returns>
    public static string Hash(byte[] content)
    {
        return Convert.ToHexStringLower(SHA256.HashData(content));
    }

    private static void AddEntry(ZipArchive archive, string name, byte[] content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var stream = entry.Open();
        stream.Write(content);
    }
}
