namespace Tankradar.MAUI.Models.Backup;

/// <summary>
/// Eine vom Nutzer gewählte externe Sicherungsdatei vor dem Einspielen.
/// </summary>
public sealed class PickedBackupFile
{
    private readonly Func<Task<Stream>> _openRead;

    /// <summary>
    /// Erstellt die Datei.
    /// </summary>
    /// <param name="fileName">Der Name der Datei.</param>
    /// <param name="openRead">Öffnet den Inhalt zum Lesen.</param>
    public PickedBackupFile(string fileName, Func<Task<Stream>> openRead)
    {
        ArgumentNullException.ThrowIfNull(openRead);
        FileName = fileName ?? string.Empty;
        _openRead = openRead;
    }

    /// <summary>
    /// Der Name der Datei.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Öffnet den Inhalt der Datei zum Lesen.
    /// </summary>
    /// <returns>Der Datenstrom; der Aufrufer schließt ihn.</returns>
    public Task<Stream> OpenReadAsync()
    {
        return _openRead();
    }
}
