using System.IO.Compression;
using System.Security.Cryptography;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Schreibt das Sicherungsarchiv: zuerst die Datenbank, dann die Belegfotos, zuletzt das Manifest mit Größen und SHA-256-Prüfsummen.
/// </summary>
public static class BackupArchiveWriter
{
    private const int BufferSize = 81_920;

    /// <summary>
    /// Schreibt das Archiv in die angegebene Datei.
    /// </summary>
    /// <param name="zipPath">Der Pfad der zu schreibenden Datei (wird überschrieben).</param>
    /// <param name="snapshotDatabasePath">Der Pfad der konsistenten Datenbankkopie.</param>
    /// <param name="receiptsDirectory">Das Verzeichnis der Belegfotos; darf fehlen.</param>
    /// <param name="header">Das Manifest ohne Dateiliste (App-Version, Migration, Zeitpunkt, Zählwerte der Datenbank).</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das fertige Manifest mit Dateiliste und Zahl der Belegfotos.</returns>
    public static async Task<BackupManifest> WriteAsync(
        string zipPath,
        string snapshotDatabasePath,
        string receiptsDirectory,
        BackupManifest header,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(header);
        var manifest = new BackupManifest
        {
            AppVersion = header.AppVersion,
            DatabaseMigration = header.DatabaseMigration,
            CreatedUtc = header.CreatedUtc,
            Counts = new BackupManifestCounts
            {
                Vehicles = header.Counts.Vehicles,
                Entries = header.Counts.Entries,
                Groups = header.Counts.Groups,
                Favorites = header.Counts.Favorites,
                Stations = header.Counts.Stations,
                Prices = header.Counts.Prices,
            },
        };

        await using var file = new FileStream(zipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, BufferSize, useAsync: true);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: false);

        manifest.Files.Add(await AddFileAsync(archive, BackupFormat.DatabaseEntryName, snapshotDatabasePath, CompressionLevel.Optimal, cancellationToken).ConfigureAwait(false));

        if (Directory.Exists(receiptsDirectory))
        {
            foreach (var path in Directory.EnumerateFiles(receiptsDirectory).OrderBy(Path.GetFileName, StringComparer.Ordinal))
            {
                var name = Path.GetFileName(path);
                if (!ReceiptPhotoStore.IsValidFileName(name))
                {
                    continue;
                }

                manifest.Files.Add(await AddFileAsync(archive, BackupFormat.ReceiptEntryPrefix + name, path, CompressionLevel.NoCompression, cancellationToken).ConfigureAwait(false));
                manifest.Counts.Photos++;
            }
        }

        var manifestEntry = archive.CreateEntry(BackupFormat.ManifestEntryName, CompressionLevel.Optimal);
        await using (var manifestStream = manifestEntry.Open())
        {
            await manifestStream.WriteAsync(manifest.ToJson(), cancellationToken).ConfigureAwait(false);
        }

        return manifest;
    }

    private static async Task<BackupManifestFile> AddFileAsync(
        ZipArchive archive,
        string entryName,
        string sourcePath,
        CompressionLevel level,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryName, level);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long size = 0;
        await using (var target = entry.Open())
        await using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, BufferSize, useAsync: true))
        {
            var buffer = new byte[BufferSize];
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                hash.AppendData(buffer, 0, read);
                size += read;
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
        }

        return new BackupManifestFile { Path = entryName, Size = size, Sha256 = Convert.ToHexStringLower(hash.GetHashAndReset()) };
    }
}
