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
    private readonly Func<string> _defaultCacheDirectoryFactory;

    /// <summary>
    /// Erstellt eine neue <see cref="AppDataPathProvider"/>-Instanz mit dem Produktionsdefault
    /// <see cref="FileSystem.AppDataDirectory"/> für das Standardverzeichnis.
    /// </summary>
    public AppDataPathProvider()
        : this(() => FileSystem.AppDataDirectory, () => FileSystem.CacheDirectory)
    {
    }

    /// <summary>
    /// Erstellt eine neue <see cref="AppDataPathProvider"/>-Instanz mit einer injizierbaren
    /// Factory für das Standardverzeichnis, sofern <see cref="TestDataPaths.TestDataPathEnvironmentVariable"/>
    /// nicht gesetzt ist.
    /// </summary>
    /// <param name="defaultDirectoryFactory">Liefert das Standardverzeichnis, falls die Umgebungsvariable nicht gesetzt ist.</param>
    /// <param name="defaultCacheDirectoryFactory">Liefert das Zwischenspeicher-Verzeichnis, falls die Umgebungsvariable nicht gesetzt ist; ohne Angabe wird <see cref="FileSystem.CacheDirectory"/> verwendet.</param>
    public AppDataPathProvider(Func<string> defaultDirectoryFactory, Func<string>? defaultCacheDirectoryFactory = null)
    {
        _defaultDirectoryFactory = defaultDirectoryFactory;
        _defaultCacheDirectoryFactory = defaultCacheDirectoryFactory ?? (() => FileSystem.CacheDirectory);
    }

    /// <inheritdoc />
    public string GetDataDirectory()
    {
        var testDataPath = Environment.GetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable);

        return !string.IsNullOrWhiteSpace(testDataPath)
            ? testDataPath
            : _defaultDirectoryFactory();
    }

    /// <inheritdoc />
    public string GetCacheDirectory()
    {
        var testDataPath = Environment.GetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable);

        return !string.IsNullOrWhiteSpace(testDataPath)
            ? testDataPath
            : _defaultCacheDirectoryFactory();
    }
}
