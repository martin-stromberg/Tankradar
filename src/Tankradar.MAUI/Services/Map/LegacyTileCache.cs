namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Entfernt den Kachelspeicher früherer Versionen aus dem Datenverzeichnis (<c>&lt;AppData&gt;/tiles</c>); seit Schritt 10 liegt er im Cache-Verzeichnis.
/// </summary>
public static class LegacyTileCache
{
    /// <summary>
    /// Löscht das alte Verzeichnis im Hintergrund (bestmöglich, Fehler bleiben folgenlos), sofern es nicht das aktuelle Verzeichnis ist.
    /// </summary>
    /// <param name="legacyDirectory">Das alte Verzeichnis.</param>
    /// <param name="currentDirectory">Das aktuelle Verzeichnis des Kachelspeichers.</param>
    public static void DeleteInBackground(string legacyDirectory, string currentDirectory)
    {
        _ = Task.Run(() => Delete(legacyDirectory, currentDirectory));
    }

    /// <summary>
    /// Löscht das alte Verzeichnis (bestmöglich), sofern es nicht das aktuelle Verzeichnis ist.
    /// </summary>
    /// <param name="legacyDirectory">Das alte Verzeichnis.</param>
    /// <param name="currentDirectory">Das aktuelle Verzeichnis des Kachelspeichers.</param>
    /// <returns><see langword="true"/>, wenn das alte Verzeichnis existierte und gelöscht wurde.</returns>
    public static bool Delete(string legacyDirectory, string currentDirectory)
    {
        try
        {
            if (string.Equals(Path.GetFullPath(legacyDirectory), Path.GetFullPath(currentDirectory), StringComparison.OrdinalIgnoreCase) || !Directory.Exists(legacyDirectory))
            {
                return false;
            }

            Directory.Delete(legacyDirectory, recursive: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Reiner Platzgewinn; ein Fehler wird beim nächsten Start erneut versucht.
            return false;
        }
    }
}
