namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass <see cref="TestDataContext"/> ein isoliertes Testdatenverzeichnis anlegt und wieder aufräumt.
/// </summary>
public class TestDataContextTests_Lifecycle
{
    /// <summary>
    /// Prüft, dass beim Erstellen des Kontexts das isolierte Datenverzeichnis angelegt wird.
    /// </summary>
    [Fact]
    public void Constructor_CreatesIsolatedDataDirectory()
    {
        using var context = new TestDataContext();

        Assert.True(Directory.Exists(context.DataDirectory));
    }

    /// <summary>
    /// Prüft, dass <see cref="TestDataContext.Cleanup"/> das Datenverzeichnis wieder entfernt.
    /// </summary>
    [Fact]
    public void Cleanup_RemovesDataDirectory()
    {
        var context = new TestDataContext();

        context.Cleanup();

        Assert.False(Directory.Exists(context.DataDirectory));
    }

    /// <summary>
    /// Prüft, dass jeder Testkontext ein eigenes, eindeutiges Datenverzeichnis erhält.
    /// </summary>
    [Fact]
    public void Constructor_CreatesUniqueDirectoryPerInstance()
    {
        using var contextA = new TestDataContext();
        using var contextB = new TestDataContext();

        Assert.NotEqual(contextA.DataDirectory, contextB.DataDirectory);
    }
}
