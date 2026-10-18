using Tankradar.MAUI.Models.Backup;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Umsetzung von <see cref="IRestoreRecovery"/> anhand des Journals <c>restore.journal</c>.
/// </summary>
public sealed class RestoreRecovery : IRestoreRecovery
{
    /// <inheritdoc />
    public void RecoverIfNeeded(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataDirectory);
        if (!Directory.Exists(dataDirectory))
        {
            return;
        }

        if (RestoreJournal.Exists(dataDirectory))
        {
            try
            {
                RestoreSwap.Undo(dataDirectory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Die Migration darf nicht auf dem Zwischenstand laufen (sie legte sonst eine leere Datenbank an, die der nächste Start überschreibt).
                // Journal und Rücksicherung bleiben bestehen; die Initialisierung scheitert und kann erneut versucht werden, der nächste Start versucht es ebenfalls.
                throw new IOException("Die unterbrochene Wiederherstellung konnte nicht zurückgenommen werden. Die Daten bleiben gesichert; bitte die App neu starten.", ex);
            }
        }

        BackupFileCleanup.DeleteDirectory(Path.Combine(dataDirectory, BackupFormat.StagingDirectoryName));
        if (!RestoreJournal.Exists(dataDirectory))
        {
            BackupFileCleanup.DeleteDirectory(Path.Combine(dataDirectory, BackupFormat.RollbackDirectoryName));
        }
    }
}
