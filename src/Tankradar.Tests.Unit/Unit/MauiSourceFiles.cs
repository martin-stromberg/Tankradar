namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Findet Quelldateien des MAUI-Projekts im Repository, damit Tests Ressourcen (Icons, Info.plist, Datenschutz-Manifest) prüfen können.
/// </summary>
internal static class MauiSourceFiles
{
    /// <summary>
    /// Ermittelt das Repository-Root anhand der Lösungsdatei, ausgehend vom Ausgabeverzeichnis des Tests.
    /// </summary>
    /// <returns>Der Pfad des Repository-Roots.</returns>
    public static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Tankradar.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }

    /// <summary>
    /// Gibt den vollständigen Pfad einer Datei unterhalb von <c>src/Tankradar.MAUI</c> zurück.
    /// </summary>
    /// <param name="relativePath">Pfadsegmente relativ zum MAUI-Projektordner.</param>
    /// <returns>Der vollständige Pfad.</returns>
    public static string PathOf(params string[] relativePath)
    {
        return Path.Combine([RepositoryRoot(), "src", "Tankradar.MAUI", .. relativePath]);
    }

    /// <summary>
    /// Liest eine Textdatei unterhalb von <c>src/Tankradar.MAUI</c>.
    /// </summary>
    /// <param name="relativePath">Pfadsegmente relativ zum MAUI-Projektordner.</param>
    /// <returns>Der Dateiinhalt.</returns>
    public static string Read(params string[] relativePath)
    {
        return File.ReadAllText(PathOf(relativePath));
    }
}
