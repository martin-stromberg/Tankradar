using System.IO.Compression;
using System.Security.Cryptography;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Prüft ein Sicherungsarchiv vor jedem Eingriff in die Daten (Eintragsnamen gegen die Whitelist, Grenzen, Manifest, Versionsregeln, Prüfsummen)
/// und entpackt es in ein Staging-Verzeichnis. Die Eintragsnamen werden nie als Pfad verwendet, sondern gegen feste Muster geprüft (Zip-Slip-Schutz).
/// </summary>
public static class BackupArchiveValidator
{
    /// <summary>
    /// Prüft, ob ein Eintragsname erlaubt ist: <c>manifest.json</c>, <c>database/tankatlas.db</c> oder <c>receipts/&lt;gültiger Fotoname&gt;</c>.
    /// </summary>
    /// <param name="name">Der Name im Archiv.</param>
    /// <returns><see langword="true"/>, wenn der Name erlaubt ist.</returns>
    public static bool IsValidEntryName(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (name == BackupFormat.ManifestEntryName || name == BackupFormat.DatabaseEntryName)
        {
            return true;
        }

        return name.StartsWith(BackupFormat.ReceiptEntryPrefix, StringComparison.Ordinal)
            && ReceiptPhotoStore.IsValidFileName(name[BackupFormat.ReceiptEntryPrefix.Length..]);
    }

    /// <summary>
    /// Prüft die Eintragsnamen eines Archivs: nur erlaubte Namen, keine Duplikate (ohne Beachtung der Groß- und Kleinschreibung), Manifest und Datenbank je genau einmal.
    /// </summary>
    /// <param name="names">Die Namen aller Einträge.</param>
    /// <returns><see langword="true"/>, wenn die Namen zulässig sind.</returns>
    public static bool AreEntryNamesValid(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            if (!IsValidEntryName(name) || !seen.Add(name))
            {
                return false;
            }
        }

        return seen.Contains(BackupFormat.ManifestEntryName) && seen.Contains(BackupFormat.DatabaseEntryName);
    }

    /// <summary>
    /// Prüft Format, Format- und Datenbankversion eines Manifests gegen die App.
    /// </summary>
    /// <param name="manifest">Das Manifest.</param>
    /// <param name="knownMigrations">Die Migrationen der App (aufsteigend nach Kennung).</param>
    /// <returns><see cref="BackupResult.Ok"/>, wenn die Sicherung eingespielt werden darf; <see cref="BackupResult.TooNew"/>, wenn sie aus einer neueren Version stammt; sonst <see cref="BackupResult.InvalidFile"/>.</returns>
    public static BackupResult CheckVersion(BackupManifest manifest, IReadOnlyList<string> knownMigrations)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(knownMigrations);

        if (manifest.Format != BackupFormat.FormatName || manifest.SchemaVersion < 1)
        {
            return BackupResult.InvalidFile;
        }

        if (manifest.SchemaVersion > BackupFormat.SchemaVersion)
        {
            return BackupResult.TooNew;
        }

        var migration = manifest.DatabaseMigration;
        if (string.IsNullOrWhiteSpace(migration))
        {
            return BackupResult.InvalidFile;
        }

        if (knownMigrations.Contains(migration, StringComparer.Ordinal))
        {
            return BackupResult.Ok;
        }

        var latest = knownMigrations.Count == 0 ? string.Empty : knownMigrations.Max(StringComparer.Ordinal) ?? string.Empty;
        return string.CompareOrdinal(migration, latest) > 0 ? BackupResult.TooNew : BackupResult.InvalidFile;
    }

    /// <summary>
    /// Liest das Manifest einer Sicherung, ohne die übrigen Einträge zu prüfen.
    /// </summary>
    /// <param name="zipPath">Der Pfad der Datei.</param>
    /// <returns>Das Manifest oder <see langword="null"/>, wenn die Datei kein lesbares Archiv mit gültigem Manifest ist.</returns>
    public static BackupManifest? ReadManifest(string zipPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            return ReadManifest(archive);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Prüft die Struktur einer Sicherung vollständig bis auf die Prüfsummen der Inhalte: Grenzen, Eintragsnamen, Manifest, Dateiliste, Versionsregeln.
    /// Es werden keine Dateien geschrieben.
    /// </summary>
    /// <param name="zipPath">Der Pfad der Datei.</param>
    /// <param name="knownMigrations">Die Migrationen der App (aufsteigend nach Kennung).</param>
    /// <returns>Das Ergebnis; bei <see cref="BackupResult.Ok"/> und <see cref="BackupResult.TooNew"/> mit Manifest.</returns>
    public static BackupOutcome Validate(string zipPath, IReadOnlyList<string> knownMigrations)
    {
        ArgumentNullException.ThrowIfNull(knownMigrations);
        try
        {
            if (new FileInfo(zipPath).Length == 0)
            {
                return new BackupOutcome(BackupResult.InvalidFile);
            }

            using var archive = ZipFile.OpenRead(zipPath);
            if (archive.Entries.Count > BackupLimits.MaxEntries)
            {
                return new BackupOutcome(BackupResult.InvalidFile);
            }

            var manifest = ReadManifest(archive);
            if (manifest is null)
            {
                return new BackupOutcome(BackupResult.InvalidFile);
            }

            var version = CheckVersion(manifest, knownMigrations);
            if (version != BackupResult.Ok)
            {
                return new BackupOutcome(version, null, version == BackupResult.TooNew ? manifest : null);
            }

            return CheckStructure(archive, manifest) ? new BackupOutcome(BackupResult.Ok, null, manifest) : new BackupOutcome(BackupResult.InvalidFile);
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException)
        {
            return new BackupOutcome(BackupResult.InvalidFile);
        }
    }

    /// <summary>
    /// Liest alle Einträge und vergleicht Größe und SHA-256-Prüfsumme mit dem Manifest, ohne etwas zu speichern.
    /// </summary>
    /// <param name="zipPath">Der Pfad der Datei.</param>
    /// <param name="manifest">Das zuvor geprüfte Manifest.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns><see cref="BackupResult.Ok"/> oder <see cref="BackupResult.InvalidFile"/>.</returns>
    public static async Task<BackupResult> VerifyContentAsync(string zipPath, BackupManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var file in manifest.Files)
            {
                var entry = archive.GetEntry(file.Path);
                if (entry is null || !await CopyVerifiedAsync(entry, file, Stream.Null, cancellationToken).ConfigureAwait(false))
                {
                    return BackupResult.InvalidFile;
                }
            }

            return BackupResult.Ok;
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException)
        {
            return BackupResult.InvalidFile;
        }
    }

    /// <summary>
    /// Entpackt die Datenbank und die Belegfotos in das Staging-Verzeichnis und prüft dabei Größe, Prüfsumme und Fotosignatur.
    /// Ziele sind <c>tankatlas.db</c> und <c>receipts/&lt;Name&gt;</c> unterhalb des Verzeichnisses.
    /// </summary>
    /// <param name="zipPath">Der Pfad der Datei.</param>
    /// <param name="manifest">Das zuvor geprüfte Manifest.</param>
    /// <param name="stagingDirectory">Das Zielverzeichnis (wird angelegt).</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns><see cref="BackupResult.Ok"/> oder <see cref="BackupResult.InvalidFile"/>.</returns>
    public static async Task<BackupResult> ExtractAsync(string zipPath, BackupManifest manifest, string stagingDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var root = Path.GetFullPath(stagingDirectory) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(stagingDirectory);
        long total = 0;
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var file in manifest.Files)
            {
                var entry = archive.GetEntry(file.Path);
                var target = GetTargetPath(stagingDirectory, file.Path);
                if (entry is null || target is null || !Path.GetFullPath(target).StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    return BackupResult.InvalidFile;
                }

                total += file.Size;
                if (total > BackupLimits.MaxTotalBytes)
                {
                    return BackupResult.InvalidFile;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                bool copied;
                await using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, BackupLimits.BufferSize, useAsync: true))
                {
                    copied = await CopyVerifiedAsync(entry, file, output, cancellationToken).ConfigureAwait(false);
                }

                if (!copied || (file.Path != BackupFormat.DatabaseEntryName && !HasMatchingSignature(target)))
                {
                    return BackupResult.InvalidFile;
                }
            }

            return BackupResult.Ok;
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException)
        {
            return BackupResult.InvalidFile;
        }
    }

    private static BackupManifest? ReadManifest(ZipArchive archive)
    {
        var entry = archive.GetEntry(BackupFormat.ManifestEntryName);
        if (entry is null || entry.Length > BackupLimits.MaxManifestBytes)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        using (var stream = entry.Open())
        {
            var chunk = new byte[BackupLimits.BufferSize];
            int read;
            while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
            {
                if (buffer.Length + read > BackupLimits.MaxManifestBytes)
                {
                    return null;
                }

                buffer.Write(chunk, 0, read);
            }
        }

        buffer.Position = 0;
        return BackupManifest.FromJson(buffer);
    }

    private static bool CheckStructure(ZipArchive archive, BackupManifest manifest)
    {
        if (!AreEntryNamesValid(archive.Entries.Select(entry => entry.FullName)))
        {
            return false;
        }

        long total = 0;
        foreach (var entry in archive.Entries)
        {
            total += entry.Length;
            var limit = entry.FullName switch
            {
                BackupFormat.ManifestEntryName => BackupLimits.MaxManifestBytes,
                BackupFormat.DatabaseEntryName => BackupLimits.MaxDatabaseBytes,
                _ => TankbookEntryLimits.MaxReceiptPhotoBytes,
            };
            if (entry.Length > limit)
            {
                return false;
            }
        }

        if (total > BackupLimits.MaxTotalBytes)
        {
            return false;
        }

        var expected = archive.Entries.Where(entry => entry.FullName != BackupFormat.ManifestEntryName).ToDictionary(entry => entry.FullName, StringComparer.Ordinal);
        if (manifest.Files.Count != expected.Count)
        {
            return false;
        }

        var listed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in manifest.Files)
        {
            if (file is null
                || !listed.Add(file.Path ?? string.Empty)
                || !expected.TryGetValue(file.Path ?? string.Empty, out var entry)
                || entry.Length != file.Size
                || file.Sha256 is not { Length: 64 })
            {
                return false;
            }
        }

        return true;
    }

    private static string? GetTargetPath(string stagingDirectory, string entryName)
    {
        if (entryName == BackupFormat.DatabaseEntryName)
        {
            return Path.Combine(stagingDirectory, TankradarDbContext.DatabaseFileName);
        }

        if (entryName.StartsWith(BackupFormat.ReceiptEntryPrefix, StringComparison.Ordinal))
        {
            var name = entryName[BackupFormat.ReceiptEntryPrefix.Length..];
            return ReceiptPhotoStore.IsValidFileName(name) ? Path.Combine(stagingDirectory, ReceiptPhotoStore.DirectoryName, name) : null;
        }

        return null;
    }

    private static bool HasMatchingSignature(string path)
    {
        var detected = ReceiptPhotoStore.DetectExtension(path);
        if (detected is null)
        {
            return false;
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => detected == ".jpg",
            ".heic" or ".heif" => detected is ".heic" or ".heif",
            _ => extension == detected,
        };
    }

    private static async Task<bool> CopyVerifiedAsync(ZipArchiveEntry entry, BackupManifestFile file, Stream destination, CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long size = 0;
        await using (var source = entry.Open())
        {
            var buffer = new byte[BackupLimits.BufferSize];
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                size += read;
                if (size > file.Size)
                {
                    return false;
                }

                hash.AppendData(buffer, 0, read);
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
        }

        return size == file.Size && string.Equals(Convert.ToHexStringLower(hash.GetHashAndReset()), file.Sha256, StringComparison.OrdinalIgnoreCase);
    }
}
