using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Services;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass gespeicherte Einstellungen einen simulierten Neustart (neue Factory, neuer Dienst) überstehen.
/// </summary>
public class SettingsServiceTests_RestartPersistence : IDisposable
{
    private readonly TestDatabase _database = new();

    /// <summary>
    /// Prüft, dass eine neue Dienstinstanz nach dem Leeren der Verbindungspools dieselben Werte lädt.
    /// </summary>
    [Fact]
    public async Task Save_ThenNewServiceInstance_LoadsSameValues()
    {
        var saved = new AppSettings(
            [
                new FuelTypeSelection(FuelType.Diesel, true),
                new FuelTypeSelection(FuelType.SuperE10, false),
                new FuelTypeSelection(FuelType.SuperE5, true),
            ],
            GpsUsage.Never,
            ResultView.Map,
            ResultSortOrder.Name);
        await CreateService().SaveAsync(saved);

        SqliteConnection.ClearAllPools();
        var loaded = await CreateService().LoadAsync();

        Assert.Equal(saved.FuelTypes, loaded.FuelTypes);
        Assert.Equal(saved.GpsUsage, loaded.GpsUsage);
        Assert.Equal(saved.ResultView, loaded.ResultView);
        Assert.Equal(saved.ResultSortOrder, loaded.ResultSortOrder);
    }

    /// <summary>
    /// Prüft, dass jede Standortnutzung über die Datei hinweg erhalten bleibt.
    /// </summary>
    /// <param name="gps">Die zu speichernde Standortnutzung.</param>
    [Theory]
    [InlineData(GpsUsage.Always)]
    [InlineData(GpsUsage.WhileInUse)]
    [InlineData(GpsUsage.Never)]
    public async Task Save_GpsUsage_RoundTripsThroughFile(GpsUsage gps)
    {
        await CreateService().SaveAsync(AppSettings.CreateDefault() with { GpsUsage = gps });

        var loaded = await LoadAfterRestartAsync();

        Assert.Equal(gps, loaded.GpsUsage);
    }

    /// <summary>
    /// Prüft, dass jede Ergebnisansicht über die Datei hinweg erhalten bleibt.
    /// </summary>
    /// <param name="view">Die zu speichernde Ansicht.</param>
    [Theory]
    [InlineData(ResultView.List)]
    [InlineData(ResultView.Map)]
    public async Task Save_ResultView_RoundTripsThroughFile(ResultView view)
    {
        await CreateService().SaveAsync(AppSettings.CreateDefault() with { ResultView = view });

        var loaded = await LoadAfterRestartAsync();

        Assert.Equal(view, loaded.ResultView);
    }

    /// <summary>
    /// Prüft, dass jede Ergebnissortierung über die Datei hinweg erhalten bleibt.
    /// </summary>
    /// <param name="sort">Die zu speichernde Sortierung.</param>
    [Theory]
    [InlineData(ResultSortOrder.Price)]
    [InlineData(ResultSortOrder.Distance)]
    [InlineData(ResultSortOrder.Name)]
    public async Task Save_ResultSortOrder_RoundTripsThroughFile(ResultSortOrder sort)
    {
        await CreateService().SaveAsync(AppSettings.CreateDefault() with { ResultSortOrder = sort });

        var loaded = await LoadAfterRestartAsync();

        Assert.Equal(sort, loaded.ResultSortOrder);
    }

    /// <summary>
    /// Prüft, dass jede einzeln ausgewählte Spritsorte über die Datei hinweg erhalten bleibt.
    /// </summary>
    /// <param name="fuelType">Die einzig ausgewählte Spritsorte.</param>
    [Theory]
    [InlineData(FuelType.SuperE5)]
    [InlineData(FuelType.SuperE10)]
    [InlineData(FuelType.Diesel)]
    public async Task Save_SingleSelectedFuelType_RoundTripsThroughFile(FuelType fuelType)
    {
        var fuelTypes = AppSettings.CreateDefault().FuelTypes.Select(f => f with { IsSelected = f.FuelType == fuelType }).ToList();
        await CreateService().SaveAsync(AppSettings.CreateDefault() with { FuelTypes = fuelTypes });

        var loaded = await LoadAfterRestartAsync();

        Assert.Equal(fuelTypes, loaded.FuelTypes);
    }

    /// <summary>
    /// Räumt die Testumgebung auf.
    /// </summary>
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<AppSettings> LoadAfterRestartAsync()
    {
        SqliteConnection.ClearAllPools();
        return await CreateService().LoadAsync();
    }

    private SettingsService CreateService()
    {
        var factory = _database.CreateFactory();
        return new SettingsService(factory, _database.CreateInitializer(factory));
    }
}
