using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Services;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass <see cref="TestDatabase"/> Verbindungen ohne Pool verwendet und das Testverzeichnis ohne globales Leeren der Verbindungspools löschen kann.
/// </summary>
public class TestDatabaseTests_Pooling
{
    /// <summary>
    /// Prüft, dass die Verbindungszeichenfolge der Kontexte den Pool abschaltet.
    /// </summary>
    [Fact]
    public void CreateFactory_UsesConnectionsWithoutPooling()
    {
        using var database = new TestDatabase();

        using var context = database.CreateFactory().CreateDbContext();

        Assert.Contains("Pooling=False", context.Database.GetConnectionString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Prüft, dass das Testverzeichnis nach der Nutzung der Datenbank durch einen Dienst und dem Freigeben gelöscht ist.
    /// </summary>
    [Fact]
    public async Task Dispose_AfterServiceUse_RemovesDirectoryWithoutClearingPools()
    {
        var database = new TestDatabase();
        var factory = database.CreateFactory();
        var service = new SettingsService(factory, database.CreateInitializer(factory));
        var settings = await service.LoadAsync();
        await service.SaveAsync(settings);
        Assert.True(File.Exists(database.DatabasePath));

        database.Dispose();

        Assert.False(Directory.Exists(database.DataDirectory));
    }
}
