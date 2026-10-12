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
            RestoreSwap.Undo(dataDirectory);
        }

        DeleteDirectory(Path.Combine(dataDirectory, BackupFormat.StagingDirectoryName));
        DeleteDirectory(Path.Combine(dataDirectory, BackupFormat.RollbackDirectoryName));
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
