using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den angezeigten Namen der Favoriteneinträge: Marke vorn, Stammdatenname darunter, eigener Name mit Vorrang, Ordnung nach dem angezeigten Namen.
/// </summary>
public class FavoritesServiceTests_DisplayName : BaseTest
{
    private readonly FavoritesFixture _fixture = new();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fixture.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task SetNamesAsync(string stationId, string? brand, string? customName)
    {
        await using var context = _fixture.Factory.CreateDbContext();
        var station = await context.Stations.SingleAsync(s => s.Id == stationId);
        station.Brand = brand;
        station.CustomName = customName;
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Prüft, dass ein Eintrag mit Marke die Marke als Namen und den Stammdatennamen als zweite Zeile liefert, einer ohne Marke nur den Namen.
    /// </summary>
    [Fact]
    public async Task GetEntries_ShowsBrandWithSubName_AndNameWithoutBrand()
    {
        await SetNamesAsync(FavoritesFixture.StationA, "Westfalen", null);
        var group = (await _fixture.Service.CreateGroupAsync("Arbeitsweg", null)).Group!.Id;
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA);
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationB);

        var entries = await _fixture.Service.GetEntriesAsync(group);

        var alpha = entries.Single(e => e.StationId == FavoritesFixture.StationA);
        Assert.Equal("Westfalen", alpha.StationName);
        Assert.Equal("Alpha Tankstelle", alpha.SubName);
        var beta = entries.Single(e => e.StationId == FavoritesFixture.StationB);
        Assert.Equal("Beta Tankstelle", beta.StationName);
        Assert.Equal(string.Empty, beta.SubName);
    }

    /// <summary>
    /// Prüft, dass der eigene Name Vorrang hat und die Reihenfolge ohne Priorität dem angezeigten Namen folgt.
    /// </summary>
    [Fact]
    public async Task GetEntries_CustomNameWinsAndOrdersByDisplayedName()
    {
        await SetNamesAsync(FavoritesFixture.StationA, "Westfalen", "Zuhause");
        await SetNamesAsync(FavoritesFixture.StationB, "Aral", null);
        var group = (await _fixture.Service.CreateGroupAsync("Arbeitsweg", null)).Group!.Id;
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA);
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationB);
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationC);

        var entries = await _fixture.Service.GetEntriesAsync(group);

        Assert.Equal(["Aral", "Citra Tankstelle", "Zuhause"], entries.Select(e => e.StationName));
        Assert.Equal("Alpha Tankstelle", entries.Single(e => e.StationName == "Zuhause").SubName);
    }

    /// <summary>
    /// Prüft, dass die Übersicht der Startseite denselben angezeigten Namen verwendet.
    /// </summary>
    [Fact]
    public async Task GetOverview_UsesDisplayedName()
    {
        await SetNamesAsync(FavoritesFixture.StationA, "Westfalen", null);
        var group = (await _fixture.Service.CreateGroupAsync("Heimat", null)).Group!.Id;
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA);

        var overview = await _fixture.Service.GetOverviewAsync();

        var entry = Assert.Single(Assert.Single(overview).Entries);
        Assert.Equal("Westfalen", entry.StationName);
        Assert.Equal("Alpha Tankstelle", entry.SubName);
        Assert.Equal(FavoritePriority.None, entry.Priority);
    }
}
