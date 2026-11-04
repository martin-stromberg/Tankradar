namespace PagesScreenshots;

/// <summary>
/// Erzeugt echte Screenshots der Windows-App für die GitHub-Pages-Präsentation: startet die App im Testmodus
/// (isoliertes Datenverzeichnis, Mock-Dienste, kein Netzwerkzugriff auf produktive Endpunkte) und schreibt PNG-Dateien.
/// Aufruf: dotnet run --project tools/PagesScreenshots -- [Zielverzeichnis] (Standard: pages/assets/screenshots).
/// </summary>
public static class Program
{
    /// <summary>Der Einstiegspunkt des Werkzeugs.</summary>
    /// <param name="args">Optionales Zielverzeichnis als erstes Argument.</param>
    /// <returns>0 bei Erfolg, 1 bei einem Fehler.</returns>
    public static int Main(string[] args)
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var outputDirectory = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.Combine(repositoryRoot, "pages", "assets", "screenshots");

        Console.WriteLine($"Zielverzeichnis: {outputDirectory}");

        try
        {
            using var session = new CaptureSession();
            session.CaptureAll(outputDirectory);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Screenshot-Erzeugung fehlgeschlagen: {exception}");
            return 1;
        }

        return 0;
    }
}
