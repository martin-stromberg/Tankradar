namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Gibt eine Sicherungsdatei über die Systemfunktion „Teilen/Speichern“ weiter (im Testmodus durch Kopieren in ein Testverzeichnis ersetzt).
/// </summary>
public interface IBackupSharer
{
    /// <summary>
    /// Öffnet die Systemfunktion zum Teilen oder Speichern der Datei. Die App überträgt selbst nichts.
    /// </summary>
    /// <param name="fullPath">Der Pfad der Sicherung.</param>
    /// <param name="title">Der Titel der Freigabe.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Ein Task, der nach der Freigabe abgeschlossen ist.</returns>
    Task ShareAsync(string fullPath, string title, CancellationToken cancellationToken = default);
}
