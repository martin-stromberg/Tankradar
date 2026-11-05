using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration;

/// <summary>
/// Stellt für Integrationstests ein isoliertes, vom Entwicklungs- und Echtbetrieb getrenntes Datenverzeichnis bereit.
/// </summary>
public class TestDataContext : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Erstellt einen neuen <see cref="TestDataContext"/> und legt sofort das isolierte Datenverzeichnis an.
    /// </summary>
    public TestDataContext()
    {
        DataDirectory = ResolveDataDirectory();
        Initialize();
    }

    /// <summary>
    /// Das für diesen Testlauf isolierte Datenverzeichnis.
    /// </summary>
    public string DataDirectory { get; }

    /// <summary>
    /// Legt das isolierte Datenverzeichnis an, falls es noch nicht existiert.
    /// </summary>
    public void Initialize()
    {
        Directory.CreateDirectory(DataDirectory);
    }

    /// <summary>
    /// Löscht das isolierte Datenverzeichnis samt Inhalt.
    /// </summary>
    public void Cleanup()
    {
        if (Directory.Exists(DataDirectory))
        {
            Directory.Delete(DataDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Räumt das Testdatenverzeichnis auf, sofern noch nicht geschehen.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Cleanup();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private static string ResolveDataDirectory()
    {
        var basePath = Environment.GetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(basePath))
        {
            basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Tankradar.Tests");
        }

        return Path.Combine(basePath, Guid.NewGuid().ToString("N"));
    }
}
