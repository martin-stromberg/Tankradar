namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Weitergabe für den Testmodus: kopiert die Sicherung ohne Systemdialog in ein Zielverzeichnis (<c>&lt;Daten&gt;/test-shared</c>).
/// </summary>
public sealed class TestBackupSharer : IBackupSharer
{
    private readonly string _targetDirectory;

    /// <summary>
    /// Erstellt die Weitergabe.
    /// </summary>
    /// <param name="targetDirectory">Das Zielverzeichnis (wird angelegt).</param>
    public TestBackupSharer(string targetDirectory)
    {
        _targetDirectory = targetDirectory;
    }

    /// <inheritdoc />
    public Task ShareAsync(string fullPath, string title, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Die Sicherung ist nicht vorhanden.");
        }

        Directory.CreateDirectory(_targetDirectory);
        File.Copy(fullPath, Path.Combine(_targetDirectory, Path.GetFileName(fullPath)), overwrite: true);
        return Task.CompletedTask;
    }
}
