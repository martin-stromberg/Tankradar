using Tankradar.TestSupport;

namespace Tankradar.MAUI.Services;

/// <summary>
/// Ermittelt das aktuell gültige App-Datenverzeichnis anhand der Umgebungsvariable
/// <see cref="TestDataPaths.TestDataPathEnvironmentVariable"/>, mit Fallback auf das reguläre
/// Plattform-Datenverzeichnis.
/// </summary>
public class AppDataPathProvider : IAppDataPathProvider
{
    private readonly Func<string> _defaultDirectoryFactory;

    /// <summary>
    /// Erstellt eine neue <see cref="AppDataPathProvider"/>-Instanz mit dem Produktionsdefault
    /// <see cref="FileSystem.AppDataDirectory"/> für das Standardverzeichnis.
    /// </summary>
    public AppDataPathProvider()
        : this(() => FileSystem.AppDataDirectory)
    {
    }

    /// <summary>
    /// Erstellt eine neue <see cref="AppDataPathProvider"/>-Instanz mit einer injizierbaren
    /// Factory für das Standardverzeichnis, sofern <see cref="TestDataPaths.TestDataPathEnvironmentVariable"/>
    /// nicht gesetzt ist.
    /// </summary>
    /// <param name="defaultDirectoryFactory">Liefert das Standardverzeichnis, falls die Umgebungsvariable nicht gesetzt ist.</param>
    public AppDataPathProvider(Func<string> defaultDirectoryFactory)
    {
        _defaultDirectoryFactory = defaultDirectoryFactory;
    }

    /// <inheritdoc />
    public string GetDataDirectory()
    {
        var testDataPath = Environment.GetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable);

        return !string.IsNullOrWhiteSpace(testDataPath)
            ? testDataPath
            : _defaultDirectoryFactory();
    }
}
