using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Services;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass eine Datenbank einer älteren Schemaversion beim Start ohne Datenverlust aktualisiert wird.
/// </summary>
public class DatabaseInitializerTests_Upgrade : IDisposable
{
    private readonly TestDatabase _database = new();

    /// <summary>
    /// Prüft, dass vorhandene Einstellungen der ersten Schemaversion nach der Aktualisierung erhalten bleiben.
    /// </summary>
    [Fact]
    public async Task Initialize_FromOlderSchemaVersion_KeepsData()
    {
        var factory = _database.CreateFactory();
        await using (var oldContext = factory.CreateDbContext())
        {
            var firstMigration = oldContext.Database.GetMigrations().First();
            await oldContext.GetService<IMigrator>().MigrateAsync(firstMigration);
            await oldContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO UserSettings (Id, GpsUsage, ResultView, ResultSortOrder) VALUES (1, 'Never', 'Map', 'Name')");
            Assert.Single(await oldContext.Database.GetAppliedMigrationsAsync());
        }

        var upgradeFactory = _database.CreateFactory();
        await _database.CreateInitializer(upgradeFactory).InitializeAsync();

        await using var context = upgradeFactory.CreateDbContext();
        Assert.Equal(4, (await context.Database.GetAppliedMigrationsAsync()).Count());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Empty(context.FuelTypeSettings);
        var settings = await new SettingsService(upgradeFactory, _database.CreateInitializer(upgradeFactory)).LoadAsync();
        Assert.Equal(GpsUsage.Never, settings.GpsUsage);
        Assert.Equal(ResultView.Map, settings.ResultView);
        Assert.Equal(ResultSortOrder.Name, settings.ResultSortOrder);
    }

    /// <summary>
    /// Prüft, dass ein wiederholter Start nichts verändert.
    /// </summary>
    [Fact]
    public async Task Initialize_Twice_IsIdempotent()
    {
        await _database.CreateInitializer(_database.CreateFactory()).InitializeAsync();
        var factory = _database.CreateFactory();
        var service = new SettingsService(factory, _database.CreateInitializer(factory));
        await service.SaveAsync(AppSettings.CreateDefault() with { GpsUsage = GpsUsage.Always });

        var secondFactory = _database.CreateFactory();
        await _database.CreateInitializer(secondFactory).InitializeAsync();

        await using var context = secondFactory.CreateDbContext();
        Assert.Equal(4, (await context.Database.GetAppliedMigrationsAsync()).Count());
        Assert.Equal("Always", context.UserSettings.Single().GpsUsage);
        Assert.Equal(3, context.FuelTypeSettings.Count());
    }

    /// <summary>
    /// Räumt die Testumgebung auf.
    /// </summary>
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
