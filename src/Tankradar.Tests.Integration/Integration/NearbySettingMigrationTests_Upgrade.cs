using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Favorites;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Migration der Einstellung „In der Nähe“ auf einer echten SQLite-Datei: Bestehende Daten bleiben erhalten, die neue Spalte ist standardmäßig aus und der Wert übersteht einen Neustart.
/// </summary>
public class NearbySettingMigrationTests_Upgrade : IDisposable
{
    private const string StationId = "00000001-0000-4000-8000-000000000000";

    private readonly TestDatabase _database = new();

    private async Task MigrateToBeforeNearbyAsync(IDbContextFactory<TankradarDbContext> factory)
    {
        await using var oldContext = factory.CreateDbContext();
        var before = oldContext.Database.GetMigrations().Single(m => m.EndsWith("_AddFavorites", StringComparison.Ordinal));
        await oldContext.GetService<IMigrator>().MigrateAsync(before);
        await oldContext.Database.ExecuteSqlRawAsync("INSERT INTO UserSettings (Id, GpsUsage, ResultView, ResultSortOrder) VALUES (1, 'Never', 'Map', 'Name')");
        await oldContext.Database.ExecuteSqlRawAsync("INSERT INTO FuelTypeSettings (FuelTypeKey, SortOrder, IsSelected) VALUES ('Diesel', 0, 1)");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO Stations (Id, Name, Latitude, Longitude) VALUES ('{StationId}', 'Alpha Tankstelle', 52.5, 13.4)");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO PriceEntries (StationId, FuelTypeKey, Price, RetrievedUtc) VALUES ('{StationId}', 'Diesel', '1.699', '2026-10-05 10:00:00')");
        await oldContext.Database.ExecuteSqlRawAsync("INSERT INTO FavoriteGroups (Name, CreatedUtc) VALUES ('Arbeitsweg', '2026-10-05 10:00:00')");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO FavoriteEntries (GroupId, StationId, Priority, AddedUtc) VALUES (1, '{StationId}', 'High', '2026-10-05 10:00:00')");
        Assert.DoesNotContain(await oldContext.Database.GetAppliedMigrationsAsync(), m => m.EndsWith("_AddNearbySetting", StringComparison.Ordinal));
    }

    private SettingsService CreateService()
    {
        var factory = _database.CreateFactory();
        return new SettingsService(factory, _database.CreateInitializer(factory));
    }

    /// <summary>
    /// Prüft, dass Einstellungen, Tankstellen, Preise und Favoriten nach dem Update erhalten bleiben und „In der Nähe“ aus ist.
    /// </summary>
    [Fact]
    public async Task Initialize_FromSchemaBeforeNearbySetting_KeepsExistingDataAndAddsColumn()
    {
        await MigrateToBeforeNearbyAsync(_database.CreateFactory());

        var upgradeFactory = _database.CreateFactory();
        await _database.CreateInitializer(upgradeFactory).InitializeAsync();

        await using var context = upgradeFactory.CreateDbContext();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal("Alpha Tankstelle", (await context.Stations.SingleAsync()).Name);
        Assert.Equal(1.699m, (await context.PriceEntries.SingleAsync()).Price);
        Assert.Equal("Arbeitsweg", (await context.FavoriteGroups.SingleAsync()).Name);
        Assert.Equal(StationId, (await context.FavoriteEntries.SingleAsync()).StationId);
        Assert.Equal(1, await context.FuelTypeSettings.CountAsync());
        Assert.False((await context.UserSettings.SingleAsync()).ShowNearby);
        var settings = await new SettingsService(upgradeFactory, _database.CreateInitializer(upgradeFactory)).LoadAsync();
        Assert.Equal(GpsUsage.Never, settings.GpsUsage);
        Assert.Equal(ResultView.Map, settings.ResultView);
        Assert.False(settings.ShowNearby);
    }

    /// <summary>
    /// Prüft, dass der Schalter nach dem Update gespeichert wird und einen Neustart auf derselben Datei übersteht.
    /// </summary>
    [Fact]
    public async Task NearbySetting_SurvivesRestartOnRealDatabaseFile()
    {
        await MigrateToBeforeNearbyAsync(_database.CreateFactory());
        var settings = await CreateService().LoadAsync();
        await CreateService().SaveAsync(settings with { ShowNearby = true });

        var restarted = await CreateService().LoadAsync();

        Assert.True(restarted.ShowNearby);
        Assert.Equal(GpsUsage.Never, restarted.GpsUsage);
        var factory = _database.CreateFactory();
        var favorites = new FavoritesService(factory, _database.CreateInitializer(factory), TimeProvider.System);
        Assert.Equal(new FavoriteGroup(1, "Arbeitsweg", null, 1), Assert.Single(await favorites.GetGroupsAsync()));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
