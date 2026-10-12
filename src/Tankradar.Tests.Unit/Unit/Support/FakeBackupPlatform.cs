using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IBackupFilePicker"/> für Tests: liefert eine vorgegebene Datei oder bricht ab.
/// </summary>
public sealed class FakeBackupFilePicker : IBackupFilePicker
{
    /// <summary>
    /// Die gelieferte Datei; <see langword="null"/> bedeutet Abbruch.
    /// </summary>
    public PickedBackupFile? File { get; set; }

    /// <summary>
    /// Wenn gesetzt, wird sie von <see cref="PickAsync"/> ausgelöst.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Anzahl der Aufrufe.
    /// </summary>
    public int PickCount { get; private set; }

    /// <inheritdoc />
    public Task<PickedBackupFile?> PickAsync(CancellationToken cancellationToken = default)
    {
        PickCount++;
        return Exception is null ? Task.FromResult(File) : Task.FromException<PickedBackupFile?>(Exception);
    }
}

/// <summary>
/// <see cref="IBackupSharer"/> für Tests: protokolliert die weitergegebenen Pfade.
/// </summary>
public sealed class FakeBackupSharer : IBackupSharer
{
    /// <summary>
    /// Die weitergegebenen Pfade.
    /// </summary>
    public List<string> Shared { get; } = [];

    /// <summary>
    /// Wenn gesetzt, wird sie von <see cref="ShareAsync"/> ausgelöst.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <inheritdoc />
    public Task ShareAsync(string fullPath, string title, CancellationToken cancellationToken = default)
    {
        Shared.Add(fullPath);
        return Exception is null ? Task.CompletedTask : Task.FromException(Exception);
    }
}
