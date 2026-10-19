namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Gemeinsame Helfer zum stillen Aufräumen von Dateien und Verzeichnissen der Sicherung. Fehler werden verschluckt, weil Aufräumen nie kritisch ist.
/// </summary>
public static class BackupFileCleanup
{
    /// <summary>
    /// Löscht eine Datei, ohne bei Fehlern eine Ausnahme auszulösen.
    /// </summary>
    /// <param name="path">Der Pfad; <see langword="null"/> wird ignoriert.</param>
    public static void DeleteFile(string? path)
    {
        if (path is null)
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Aufräumen ist nicht kritisch.
        }
    }

    /// <summary>
    /// Löscht ein Verzeichnis samt Inhalt, ohne bei Fehlern eine Ausnahme auszulösen.
    /// </summary>
    /// <param name="path">Der Pfad; <see langword="null"/> oder ein fehlendes Verzeichnis wird ignoriert.</param>
    /// <param name="onlyIfEmpty">Nur löschen, wenn das Verzeichnis leer ist.</param>
    public static void DeleteDirectory(string? path, bool onlyIfEmpty = false)
    {
        if (path is null || !Directory.Exists(path))
        {
            return;
        }

        try
        {
            if (onlyIfEmpty && Directory.EnumerateFileSystemEntries(path).Any())
            {
                return;
            }

            Directory.Delete(path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Aufräumen ist nicht kritisch.
        }
    }
}
