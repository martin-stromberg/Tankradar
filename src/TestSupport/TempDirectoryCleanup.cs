using System.Diagnostics;

namespace Tankradar.TestSupport;

/// <summary>
/// Best-effort-Löschung temporärer Testverzeichnisse: Registrierte Prozesse werden zuerst beendet, danach wird das Verzeichnis
/// mit Wiederholungen und wachsendem Abstand gelöscht. Restfehler werden nur diagnostisch ausgegeben statt geworfen, damit ein
/// Aufräumfehler nie den eigentlichen Test-Assert überdeckt.
/// </summary>
internal static class TempDirectoryCleanup
{
    private const int MaxDeleteAttempts = 5;
    private static readonly TimeSpan ProcessKillWaitTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Beendet optional registrierte Prozesse und löscht <paramref name="root"/> best-effort. Wirft nie — ein letzter Restfehler geht nur nach <see cref="Console.Error"/>.
    /// </summary>
    /// <param name="root">Zu löschendes Verzeichnis.</param>
    /// <param name="processesToKill">Optional registrierte Kindprozesse, die vor dem Löschen beendet werden (Kill-Sicherheitsnetz; bereits freigegebene Objekte werden toleriert).</param>
    /// <param name="normalizeAttributes">Dateiattribute vor dem Löschen auf Normal setzen (z. B. schreibgeschützte Dateien).</param>
    internal static void DeleteBestEffort(string root, IEnumerable<Process>? processesToKill = null, bool normalizeAttributes = false)
    {
        if (processesToKill is not null)
        {
            foreach (var process in processesToKill)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        if (!process.WaitForExit(ProcessKillWaitTimeout))
                        {
                            Console.Error.WriteLine(
                                $"TempDirectoryCleanup: Prozess {process.Id} lief nach Kill länger als {ProcessKillWaitTimeout.TotalSeconds}s — Löschen wird trotzdem fortgesetzt.");
                        }
                    }
                }
                catch (Exception)
                {
                    // Best-effort: bereits freigegeben (ObjectDisposedException), nicht mehr vorhanden oder Win32-Fehler dürfen das Aufräumen nicht abbrechen.
                }
            }
        }

        try
        {
            if (!Directory.Exists(root))
            {
                return;
            }

            if (normalizeAttributes)
            {
                try
                {
                    // EnumerateFiles wertet lazy aus: Ein unzugängliches Unterverzeichnis wirft erst mitten in der Schleife.
                    // Der Fehler darf den Lösch-Wiederholungslauf unten nicht überspringen — Directory.Delete kann trotzdem erfolgreich sein.
                    foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                        }
                        catch (Exception)
                        {
                            // Best-effort: eine einzelne nicht normalisierbare Datei entscheidet erst der Löschversuch.
                        }
                    }
                }
                catch (Exception)
                {
                    // Best-effort: Attribut-Normalisierung ist nur Vorbereitung, kein Muss.
                }
            }

            for (var attempt = 1; attempt <= MaxDeleteAttempts; attempt++)
            {
                try
                {
                    Directory.Delete(root, recursive: true);
                    return;
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    if (attempt == MaxDeleteAttempts)
                    {
                        Console.Error.WriteLine(
                            $"TempDirectoryCleanup: '{root}' konnte nach {MaxDeleteAttempts} Versuchen nicht gelöscht werden: {error.Message}");
                        return;
                    }

                    Thread.Sleep(100 * attempt);
                }
            }
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"TempDirectoryCleanup: '{root}' konnte nicht gelöscht werden: {error.Message}");
        }
    }
}
