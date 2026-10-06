using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Einträge einer Gruppe: Ordnung nach Priorität, Notiz und Priorität pflegen, Adresse, Validierung und die Meldungstexte aller Ergebnisse.
/// </summary>
public class FavoritesServiceTests_Entries : BaseTest
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

    private async Task<long> GroupWithAllStationsAsync()
    {
        var group = (await _fixture.Service.CreateGroupAsync("Arbeitsweg", null)).Group!.Id;
        foreach (var station in new[] { FavoritesFixture.StationC, FavoritesFixture.StationA, FavoritesFixture.StationB })
        {
            await _fixture.Service.AddStationAsync(group, station);
        }

        return group;
    }

    /// <summary>
    /// Prüft, dass Einträge ohne Priorität nach dem Namen geordnet sind und Name sowie Adresse der Tankstelle geliefert werden.
    /// </summary>
    [Fact]
    public async Task GetEntries_WithoutPriority_OrderedByNameWithAddress()
    {
        var group = await GroupWithAllStationsAsync();

        var entries = await _fixture.Service.GetEntriesAsync(group);

        Assert.Equal(["Alpha Tankstelle", "Beta Tankstelle", "Citra Tankstelle"], entries.Select(e => e.StationName));
        Assert.Equal("Hauptstraße 1, 10115 Berlin", entries[0].AddressText);
        Assert.Equal(string.Empty, entries[1].AddressText);
        Assert.All(entries, entry => Assert.Equal(FavoritePriority.None, entry.Priority));
        Assert.All(entries, entry => Assert.Null(entry.Note));
    }

    /// <summary>
    /// Prüft, dass die Liste nach der Priorität geordnet ist (hoch zuerst), bei gleicher Priorität nach dem Namen.
    /// </summary>
    [Fact]
    public async Task GetEntries_AreOrderedByPriorityThenName()
    {
        var group = await GroupWithAllStationsAsync();
        await _fixture.Service.UpdateEntryAsync(group, FavoritesFixture.StationC, null, FavoritePriority.High);
        await _fixture.Service.UpdateEntryAsync(group, FavoritesFixture.StationB, null, FavoritePriority.Low);
        await _fixture.Service.UpdateEntryAsync(group, FavoritesFixture.StationA, null, FavoritePriority.Low);

        var entries = await _fixture.Service.GetEntriesAsync(group);

        Assert.Equal(["Citra Tankstelle", "Alpha Tankstelle", "Beta Tankstelle"], entries.Select(e => e.StationName));
        Assert.Equal([FavoritePriority.High, FavoritePriority.Low, FavoritePriority.Low], entries.Select(e => e.Priority));
    }

    /// <summary>
    /// Prüft die Reihenfolge aller vier Prioritäten: hoch, mittel, niedrig, keine.
    /// </summary>
    [Fact]
    public async Task GetEntries_AllPriorities_HighToNone()
    {
        var group = await GroupWithAllStationsAsync();
        var fourth = new MAUI.Data.StationEntity { Id = "00000004-0000-4000-8000-000000000000", Name = "Delta Tankstelle" };
        await using (var context = _fixture.Factory.CreateDbContext())
        {
            context.Stations.Add(fourth);
            await context.SaveChangesAsync();
        }

        await _fixture.Service.AddStationAsync(group, fourth.Id);
        await _fixture.Service.UpdateEntryAsync(group, FavoritesFixture.StationA, null, FavoritePriority.None);
        await _fixture.Service.UpdateEntryAsync(group, FavoritesFixture.StationB, null, FavoritePriority.Low);
        await _fixture.Service.UpdateEntryAsync(group, FavoritesFixture.StationC, null, FavoritePriority.Medium);
        await _fixture.Service.UpdateEntryAsync(group, fourth.Id, null, FavoritePriority.High);

        var entries = await _fixture.Service.GetEntriesAsync(group);

        Assert.Equal([FavoritePriority.High, FavoritePriority.Medium, FavoritePriority.Low, FavoritePriority.None], entries.Select(e => e.Priority));
    }

    /// <summary>
    /// Prüft, dass Notiz und Priorität gespeichert, die Notiz getrimmt und durch leeren Text wieder entfernt wird; ein Neustart behält sie.
    /// </summary>
    [Fact]
    public async Task UpdateEntry_StoresNoteAndPriority_SurvivesRestart()
    {
        var group = await GroupWithAllStationsAsync();

        Assert.Equal(FavoriteResult.Ok, await _fixture.Service.UpdateEntryAsync(group, FavoritesFixture.StationA, "  Günstig dienstags  ", FavoritePriority.Medium));

        var restarted = _fixture.CreateService();
        var entry = (await restarted.GetEntriesAsync(group)).Single(e => e.StationId == FavoritesFixture.StationA);
        Assert.Equal("Günstig dienstags", entry.Note);
        Assert.Equal(FavoritePriority.Medium, entry.Priority);

        Assert.Equal(FavoriteResult.Ok, await _fixture.Service.UpdateEntryAsync(group, FavoritesFixture.StationA, "   ", FavoritePriority.None));
        var cleared = (await _fixture.Service.GetEntriesAsync(group)).Single(e => e.StationId == FavoritesFixture.StationA);
        Assert.Null(cleared.Note);
        Assert.Equal(FavoritePriority.None, cleared.Priority);
    }

    /// <summary>
    /// Prüft, dass zu lange Notizen und Einträge, die es nicht gibt, abgelehnt werden.
    /// </summary>
    [Fact]
    public async Task UpdateEntry_InvalidOrUnknown_IsRejected()
    {
        var group = await GroupWithAllStationsAsync();

        Assert.Equal(FavoriteResult.InvalidText, await _fixture.Service.UpdateEntryAsync(group, FavoritesFixture.StationA, new string('n', FavoriteLimits.MaxNoteLength + 1), FavoritePriority.High));
        Assert.Equal(FavoriteResult.NotMember, await _fixture.Service.UpdateEntryAsync(group, "unbekannt", "x", FavoritePriority.High));
        Assert.Equal(FavoriteResult.NotMember, await _fixture.Service.UpdateEntryAsync(999, FavoritesFixture.StationA, "x", FavoritePriority.High));
        Assert.Equal(FavoritePriority.None, (await _fixture.Service.GetEntriesAsync(group)).First().Priority);
    }

    /// <summary>
    /// Prüft, dass eine Gruppe ohne Einträge oder eine unbekannte Gruppe eine leere Liste liefern.
    /// </summary>
    [Fact]
    public async Task GetEntries_EmptyOrUnknownGroup_ReturnsEmptyList()
    {
        var group = (await _fixture.Service.CreateGroupAsync("Leer", null)).Group!.Id;

        Assert.Empty(await _fixture.Service.GetEntriesAsync(group));
        Assert.Empty(await _fixture.Service.GetEntriesAsync(12345));
    }

    /// <summary>
    /// Prüft, dass jedes Ergebnis außer „Ok“ eine Meldung hat, „Ok“ keine, und dass die Prioritäten deutsche Beschriftungen tragen.
    /// </summary>
    [Fact]
    public void Texts_CoverAllResultsAndPriorities()
    {
        Assert.Equal(string.Empty, FavoritesTexts.GetResultMessage(FavoriteResult.Ok));
        foreach (var result in Enum.GetValues<FavoriteResult>().Where(r => r != FavoriteResult.Ok))
        {
            Assert.NotEmpty(FavoritesTexts.GetResultMessage(result));
        }

        Assert.Equal(["Keine", "Niedrig", "Mittel", "Hoch"], Enum.GetValues<FavoritePriority>().Select(FavoritesTexts.GetPriorityLabel));
        Assert.Equal("Priorität: Hoch", FavoritesTexts.FormatPriority(FavoritePriority.High));
        Assert.Equal("1 Tankstelle", FavoritesTexts.FormatStationCount(1));
        Assert.Equal("0 Tankstellen", FavoritesTexts.FormatStationCount(0));
        Assert.Contains("„Heimat“", FavoritesTexts.FormatDeleteQuestion("Heimat", 2), StringComparison.Ordinal);
        Assert.Contains("keine Tankstellen", FavoritesTexts.FormatDeleteQuestion("Heimat", 0), StringComparison.Ordinal);
    }
}
