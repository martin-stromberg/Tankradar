using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Services;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass die Migration des Preis-Caches bestehende Installationen ohne Datenverlust aktualisiert.
/// </summary>
public class PriceCacheMigrationTests_Upgrade : IDisposable
{
    private readonly TestDatabase _database = new();

    /// <summary>
    /// Prüft, dass Einstellungen aus dem Stand vor dem Preis-Cache nach dem Update erhalten bleiben und die neuen Tabellen leer vorhanden sind.
    /// </summary>
    [Fact]
    public async Task Initialize_FromSchemaBeforePriceCache_KeepsSettingsAndAddsTables()
    {
        var factory = _database.CreateFactory();
        await using (var oldContext = factory.CreateDbContext())
        {
            var beforePriceCache = oldContext.Database.GetMigrations().Single(m => m.EndsWith("_AddFuelTypeSettings", StringComparison.Ordinal));
            await oldContext.GetService<IMigrator>().MigrateAsync(beforePriceCache);
            await oldContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO UserSettings (Id, GpsUsage, ResultView, ResultSortOrder) VALUES (1, 'Always', 'Map', 'Distance')");
            await oldContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO FuelTypeSettings (FuelTypeKey, SortOrder, IsSelected) VALUES ('Diesel', 0, 1), ('SuperE5', 1, 0)");
            Assert.DoesNotContain(await oldContext.Database.GetAppliedMigrationsAsync(), m => m.EndsWith("_AddPriceCache", StringComparison.Ordinal));
        }

        var upgradeFactory = _database.CreateFactory();
        await _database.CreateInitializer(upgradeFactory).InitializeAsync();

        await using var context = upgradeFactory.CreateDbContext();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Empty(context.Stations);
        Assert.Empty(context.PriceEntries);
        Assert.Equal(2, await context.FuelTypeSettings.CountAsync());
        var settings = await new SettingsService(upgradeFactory, _database.CreateInitializer(upgradeFactory)).LoadAsync();
        Assert.Equal(GpsUsage.Always, settings.GpsUsage);
        Assert.Equal(ResultView.Map, settings.ResultView);
        Assert.Equal(ResultSortOrder.Distance, settings.ResultSortOrder);
        Assert.Equal(FuelType.Diesel, settings.FuelTypes[0].FuelType);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
