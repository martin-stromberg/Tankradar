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
/// Prüft die Migration der Favoritengruppen auf einer echten SQLite-Datei: bestehende Daten bleiben erhalten, die Favoriten überstehen einen Neustart,
/// und die Datenbank erzwingt Eindeutigkeit der Gruppennamen (ohne Groß-/Kleinschreibung), Kaskade beim Löschen einer Gruppe und den Schutz der Tankstellen.
/// </summary>
public class FavoritesMigrationTests_Upgrade : IDisposable
{
    private const string StationId = "00000001-0000-4000-8000-000000000000";

    private readonly TestDatabase _database = new();

    private async Task MigrateToBeforeFavoritesAsync(IDbContextFactory<TankradarDbContext> factory)
    {
        await using var oldContext = factory.CreateDbContext();
        var before = oldContext.Database.GetMigrations().Single(m => m.EndsWith("_AddPriceCache", StringComparison.Ordinal));
        await oldContext.GetService<IMigrator>().MigrateAsync(before);
        await oldContext.Database.ExecuteSqlRawAsync("INSERT INTO UserSettings (Id, GpsUsage, ResultView, ResultSortOrder) VALUES (1, 'Never', 'Map', 'Name')");
        await oldContext.Database.ExecuteSqlRawAsync("INSERT INTO FuelTypeSettings (FuelTypeKey, SortOrder, IsSelected) VALUES ('Diesel', 0, 1)");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO Stations (Id, Name, Latitude, Longitude) VALUES ('{StationId}', 'Alpha Tankstelle', 52.5, 13.4)");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO PriceEntries (StationId, FuelTypeKey, Price, RetrievedUtc) VALUES ('{StationId}', 'Diesel', '1.699', '2026-10-05 10:00:00')");
        Assert.DoesNotContain(await oldContext.Database.GetAppliedMigrationsAsync(), m => m.EndsWith("_AddFavorites", StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft, dass Einstellungen, Tankstellen und Preise aus dem Stand vor den Favoriten nach dem Update erhalten bleiben und die neuen Tabellen leer vorhanden sind.
    /// </summary>
    [Fact]
    public async Task Initialize_FromSchemaBeforeFavorites_KeepsExistingDataAndAddsTables()
    {
        await MigrateToBeforeFavoritesAsync(_database.CreateFactory());

        var upgradeFactory = _database.CreateFactory();
        await _database.CreateInitializer(upgradeFactory).InitializeAsync();

        await using var context = upgradeFactory.CreateDbContext();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Empty(context.FavoriteGroups);
        Assert.Empty(context.FavoriteEntries);
        Assert.Equal("Alpha Tankstelle", (await context.Stations.SingleAsync()).Name);
        Assert.Equal(1.699m, (await context.PriceEntries.SingleAsync()).Price);
        Assert.Equal(1, await context.FuelTypeSettings.CountAsync());
        var settings = await new SettingsService(upgradeFactory, _database.CreateInitializer(upgradeFactory)).LoadAsync();
        Assert.Equal(GpsUsage.Never, settings.GpsUsage);
        Assert.Equal(ResultView.Map, settings.ResultView);
    }

    /// <summary>
    /// Prüft, dass Gruppen, Zuordnungen, Notizen und Prioritäten nach einem Neustart (neue Kontext-Factory auf derselben Datei) vorhanden sind – offline, ohne jeden Netzzugriff.
    /// </summary>
    [Fact]
    public async Task Favorites_SurviveRestartOnRealDatabaseFile()
    {
        await MigrateToBeforeFavoritesAsync(_database.CreateFactory());
        var factory = _database.CreateFactory();
        var service = new FavoritesService(factory, _database.CreateInitializer(factory), TimeProvider.System);

        var group = (await service.AddStationToNewGroupAsync("Arbeitsweg", StationId)).Group!;
        await service.UpdateEntryAsync(group.Id, StationId, "Morgens tanken", FavoritePriority.High);
        await service.UpdateGroupAsync(group.Id, "Pendeln", "Mo bis Fr");

        var restartedFactory = _database.CreateFactory();
        var restarted = new FavoritesService(restartedFactory, _database.CreateInitializer(restartedFactory), TimeProvider.System);
        var groups = await restarted.GetGroupsAsync();
        var entry = Assert.Single(await restarted.GetEntriesAsync(group.Id));

        Assert.Equal(new FavoriteGroup(group.Id, "Pendeln", "Mo bis Fr", 1), Assert.Single(groups));
        Assert.Equal("Alpha Tankstelle", entry.StationName);
        Assert.Equal("Morgens tanken", entry.Note);
        Assert.Equal(FavoritePriority.High, entry.Priority);
    }

    /// <summary>
    /// Prüft, dass die Datenbank selbst doppelte Gruppennamen ohne Beachtung der Groß- und Kleinschreibung verhindert.
    /// </summary>
    [Fact]
    public async Task Database_RejectsDuplicateGroupNamesCaseInsensitive()
    {
        var factory = _database.CreateFactory();
        await _database.CreateInitializer(factory).InitializeAsync();
        await using var context = factory.CreateDbContext();
        context.FavoriteGroups.Add(new FavoriteGroupEntity { Name = "Urlaub", CreatedUtc = DateTime.UtcNow });
        await context.SaveChangesAsync();

        await using var second = factory.CreateDbContext();
        second.FavoriteGroups.Add(new FavoriteGroupEntity { Name = "URLAUB", CreatedUtc = DateTime.UtcNow });

        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    /// <summary>
    /// Prüft, dass das Löschen einer Gruppe ihre Zuordnungen mitnimmt, die Tankstelle aber behält, und dass eine zugeordnete Tankstelle nicht gelöscht werden kann.
    /// </summary>
    [Fact]
    public async Task Database_CascadesGroupDeleteAndProtectsStations()
    {
        await MigrateToBeforeFavoritesAsync(_database.CreateFactory());
        var factory = _database.CreateFactory();
        var service = new FavoritesService(factory, _database.CreateInitializer(factory), TimeProvider.System);
        var group = (await service.AddStationToNewGroupAsync("Heimat", StationId)).Group!;

        await using (var context = factory.CreateDbContext())
        {
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                context.Stations.Remove(await context.Stations.SingleAsync());
                await context.SaveChangesAsync();
            });
        }

        await service.DeleteGroupAsync(group.Id);

        await using var check = factory.CreateDbContext();
        Assert.Empty(check.FavoriteEntries);
        Assert.Single(check.Stations);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
