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
            catch (Exception)
            {
                // Der Start darf daran nicht scheitern. Journal und Rücksicherung bleiben bestehen; der nächste Start versucht es erneut.
            }
        }

        BackupFileCleanup.DeleteDirectory(Path.Combine(dataDirectory, BackupFormat.StagingDirectoryName));
        if (!RestoreJournal.Exists(dataDirectory))
        {
            BackupFileCleanup.DeleteDirectory(Path.Combine(dataDirectory, BackupFormat.RollbackDirectoryName));
        }
    }
}
