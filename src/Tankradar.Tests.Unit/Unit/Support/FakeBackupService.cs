using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IBackupService"/> für Tests: hält eine Liste im Speicher, liefert vorgegebene Ergebnisse und protokolliert die Aufrufe.
/// </summary>
public sealed class FakeBackupService : IBackupService
{
    /// <summary>
    /// Erstellt den Dienst mit einem Standardmanifest und einer Standarddatei für den Import.
    /// </summary>
    public FakeBackupService()
    {
        Manifest = new BackupManifest { AppVersion = "0.1.19", Counts = new BackupManifestCounts { Vehicles = 1, Entries = 2, Groups = 3, Favorites = 4, Stations = 5, Prices = 6, Photos = 1 } };
        ImportedFile = new BackupFileInfo("tankatlas-backup-20261012-130000.zip", new DateTime(2026, 10, 12, 13, 0, 0, DateTimeKind.Utc), 2048, "0.1.19", true);
    }

    /// <summary>
    /// Die Liste, die <see cref="ListAsync"/> liefert (neueste zuerst).
    /// </summary>
    public List<BackupFileInfo> Files { get; set; } = [];

    /// <summary>
    /// Das Ergebnis von <see cref="CreateAsync"/>; bei <see cref="BackupResult.Ok"/> wird eine Sicherung an den Anfang der Liste gestellt.
    /// </summary>
    public BackupResult CreateResult { get; set; } = BackupResult.Ok;

    /// <summary>
    /// Das Ergebnis von <see cref="RestoreAsync"/>.
    /// </summary>
    public BackupResult RestoreResult { get; set; } = BackupResult.Ok;

    /// <summary>
    /// Das Ergebnis von <see cref="DeleteAsync"/>; bei <see cref="BackupResult.Ok"/> wird die Datei aus der Liste entfernt.
    /// </summary>
    public BackupResult DeleteResult { get; set; } = BackupResult.Ok;

    /// <summary>
    /// Das Ergebnis von <see cref="InspectAsync"/>.
    /// </summary>
    public BackupResult InspectResult { get; set; } = BackupResult.Ok;

    /// <summary>
    /// Das Manifest, das <see cref="InspectAsync"/> bei Erfolg und bei <see cref="BackupResult.TooNew"/> liefert.
    /// </summary>
    public BackupManifest Manifest { get; set; }

    /// <summary>
    /// Das Ergebnis von <see cref="ImportAsync"/>; bei <see cref="BackupResult.Ok"/> wird <see cref="ImportedFile"/> in die Liste aufgenommen.
    /// </summary>
    public BackupResult ImportResult { get; set; } = BackupResult.Ok;

    /// <summary>
    /// Die Sicherung, die ein erfolgreicher Import liefert.
    /// </summary>
    public BackupFileInfo ImportedFile { get; set; }

    /// <summary>
    /// Wenn gesetzt, wird sie von jeder Operation ausgelöst.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gibt an, ob <see cref="GetFullPath"/> einen Pfad liefert.
    /// </summary>
    public bool PathAvailable { get; set; } = true;

    /// <summary>
    /// Anzahl der Aufrufe von <see cref="ListAsync"/>.
    /// </summary>
    public int ListCount { get; private set; }

    /// <summary>
    /// Anzahl der Aufrufe von <see cref="CreateAsync"/>.
    /// </summary>
    public int CreateCount { get; private set; }

    /// <summary>
    /// Die Dateinamen der Aufrufe von <see cref="RestoreAsync"/>.
    /// </summary>
    public List<string> Restored { get; } = [];

    /// <summary>
    /// Die Dateinamen der Aufrufe von <see cref="DeleteAsync"/>.
    /// </summary>
    public List<string> Deleted { get; } = [];

    /// <summary>
    /// Die Dateinamen der Aufrufe von <see cref="InspectAsync"/>.
    /// </summary>
    public List<string> Inspected { get; } = [];

    /// <summary>
    /// Die Dateinamen der eingespielten Dateien.
    /// </summary>
    public List<string> Imported { get; } = [];

    /// <summary>
    /// Wird, wenn gesetzt, am Anfang von <see cref="CreateAsync"/> abgewartet (Sperrtests).
    /// </summary>
    public TaskCompletionSource? CreateGate { get; set; }

    /// <inheritdoc />
    public async Task<BackupOutcome> CreateAsync(CancellationToken cancellationToken = default)
    {
        CreateCount++;
        if (CreateGate is not null)
        {
            await CreateGate.Task;
        }

        if (Exception is not null)
        {
            throw Exception;
        }

        if (CreateResult != BackupResult.Ok)
        {
            return new BackupOutcome(CreateResult);
        }

        var file = new BackupFileInfo("tankatlas-backup-20261012-14" + (Files.Count + 10) + "00.zip", new DateTime(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc).AddMinutes(Files.Count), 1024, "0.1.19", true);
        Files.Insert(0, file);
        return new BackupOutcome(BackupResult.Ok, file);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<BackupFileInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        ListCount++;
        if (Exception is not null)
        {
            return Task.FromException<IReadOnlyList<BackupFileInfo>>(Exception);
        }

        return Task.FromResult<IReadOnlyList<BackupFileInfo>>(Files.ToList());
    }

    /// <inheritdoc />
    public Task<BackupOutcome> InspectAsync(string fileName, CancellationToken cancellationToken = default)
    {
        Inspected.Add(fileName);
        var manifest = InspectResult is BackupResult.Ok or BackupResult.TooNew ? Manifest : null;
        return Task.FromResult(new BackupOutcome(InspectResult, null, manifest));
    }

    /// <inheritdoc />
    public Task<BackupResult> RestoreAsync(string fileName, CancellationToken cancellationToken = default)
    {
        Restored.Add(fileName);
        return Exception is null ? Task.FromResult(RestoreResult) : Task.FromException<BackupResult>(Exception);
    }

    /// <inheritdoc />
    public Task<BackupOutcome> ImportAsync(PickedBackupFile file, CancellationToken cancellationToken = default)
    {
        Imported.Add(file.FileName);
        if (ImportResult == BackupResult.Ok)
        {
            Files.Insert(0, ImportedFile);
            return Task.FromResult(new BackupOutcome(BackupResult.Ok, ImportedFile));
        }

        return Task.FromResult(new BackupOutcome(ImportResult, null, ImportResult == BackupResult.TooNew ? Manifest : null));
    }

    /// <inheritdoc />
    public Task<BackupResult> DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        Deleted.Add(fileName);
        if (Exception is not null)
        {
            return Task.FromException<BackupResult>(Exception);
        }

        if (DeleteResult is BackupResult.Ok or BackupResult.NotFound)
        {
            Files.RemoveAll(file => file.FileName == fileName);
        }

        return Task.FromResult(DeleteResult);
    }

    /// <inheritdoc />
    public string? GetFullPath(string fileName)
    {
        return PathAvailable ? Path.Combine("C:\\daten\\backups", fileName) : null;
    }
}
