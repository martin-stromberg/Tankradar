using Tankradar.MAUI.Models.Favorites;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Gesamtübersicht aller Gruppen mit ihren Einträgen in einem Zugriff.
/// </summary>
public class FavoritesServiceTests_Overview : BaseTest
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

    /// <summary>
    /// Prüft, dass Gruppen nach Namen und Einträge nach Priorität, dann Namen geliefert werden.
    /// </summary>
    [Fact]
    public async Task GetOverview_ReturnsAllGroupsWithEntriesInOrder()
    {
        var work = (await _fixture.Service.CreateGroupAsync("Arbeitsweg", "Täglich")).Group!.Id;
        var home = (await _fixture.Service.CreateGroupAsync("Heimat", null)).Group!.Id;
        await _fixture.Service.AddStationAsync(home, FavoritesFixture.StationA);
        await _fixture.Service.AddStationAsync(work, FavoritesFixture.StationA);
        await _fixture.Service.AddStationAsync(work, FavoritesFixture.StationB);
        await _fixture.Service.UpdateEntryAsync(work, FavoritesFixture.StationB, "billig", FavoritePriority.High);

        var overview = await _fixture.Service.GetOverviewAsync();

        Assert.Equal(["Arbeitsweg", "Heimat"], overview.Select(o => o.Group.Name));
        Assert.Equal(["Beta Tankstelle", "Alpha Tankstelle"], overview[0].Entries.Select(e => e.StationName));
        Assert.Equal("billig", overview[0].Entries[0].Note);
        Assert.Equal(2, overview[0].Group.StationCount);
        Assert.Equal(["Alpha Tankstelle"], overview[1].Entries.Select(e => e.StationName));
    }

    /// <summary>
    /// Prüft, dass eine Gruppe ohne Tankstellen mit leerer Eintragsliste erscheint.
    /// </summary>
    [Fact]
    public async Task GetOverview_EmptyGroup_HasNoEntries()
    {
        await _fixture.Service.CreateGroupAsync("Leer", null);

        var overview = await _fixture.Service.GetOverviewAsync();

        Assert.Empty(Assert.Single(overview).Entries);
    }

    /// <summary>
    /// Prüft, dass eine Tankstelle in zwei Gruppen in beiden erscheint.
    /// </summary>
    [Fact]
    public async Task GetOverview_StationInTwoGroups_AppearsInBoth()
    {
        var a = (await _fixture.Service.CreateGroupAsync("A", null)).Group!.Id;
        var b = (await _fixture.Service.CreateGroupAsync("B", null)).Group!.Id;
        await _fixture.Service.AddStationAsync(a, FavoritesFixture.StationC);
        await _fixture.Service.AddStationAsync(b, FavoritesFixture.StationC);

        var overview = await _fixture.Service.GetOverviewAsync();

        Assert.All(overview, o => Assert.Equal(FavoritesFixture.StationC, Assert.Single(o.Entries).StationId));
    }

    /// <summary>
    /// Prüft, dass ohne Gruppen eine leere Übersicht geliefert wird.
    /// </summary>
    [Fact]
    public async Task GetOverview_NoGroups_IsEmpty()
    {
        Assert.Empty(await _fixture.Service.GetOverviewAsync());
    }
}
