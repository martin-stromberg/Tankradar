using Tankradar.MAUI.Models.Favorites;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Zuordnung von Tankstellen zu Gruppen: Hinzufügen, Mehrfachzuordnung, Anlegen und Hinzufügen in einem Schritt und Entfernen.
/// </summary>
public class FavoritesServiceTests_Membership : BaseTest
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

    private async Task<long> NewGroupAsync(string name)
    {
        return (await _fixture.Service.CreateGroupAsync(name, null)).Group!.Id;
    }

    /// <summary>
    /// Prüft, dass eine Tankstelle einer Gruppe zugeordnet wird und die Gruppe sie zählt.
    /// </summary>
    [Fact]
    public async Task AddStation_AssignsStationToGroup()
    {
        var group = await NewGroupAsync("Arbeitsweg");

        Assert.Equal(FavoriteResult.Ok, await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA));

        Assert.Equal(1, (await _fixture.Service.GetGroupAsync(group))!.StationCount);
        Assert.Equal(["Arbeitsweg"], (await _fixture.Service.GetGroupsOfStationAsync(FavoritesFixture.StationA)).Select(g => g.Name));
    }

    /// <summary>
    /// Prüft, dass eine Tankstelle mehreren Gruppen angehören darf, je Gruppe aber nur einmal.
    /// </summary>
    [Fact]
    public async Task AddStation_MultipleGroups_AllowedDuplicateInGroupRejected()
    {
        var work = await NewGroupAsync("Arbeitsweg");
        var home = await NewGroupAsync("Heimat");

        Assert.Equal(FavoriteResult.Ok, await _fixture.Service.AddStationAsync(work, FavoritesFixture.StationA));
        Assert.Equal(FavoriteResult.Ok, await _fixture.Service.AddStationAsync(home, FavoritesFixture.StationA));
        Assert.Equal(FavoriteResult.AlreadyMember, await _fixture.Service.AddStationAsync(work, FavoritesFixture.StationA));

        Assert.Equal(["Arbeitsweg", "Heimat"], (await _fixture.Service.GetGroupsOfStationAsync(FavoritesFixture.StationA)).Select(g => g.Name));
        Assert.Equal(1, (await _fixture.Service.GetGroupAsync(work))!.StationCount);
    }

    /// <summary>
    /// Prüft, dass unbekannte Tankstellen und Gruppen abgelehnt werden.
    /// </summary>
    [Fact]
    public async Task AddStation_UnknownStationOrGroup_IsRejected()
    {
        var group = await NewGroupAsync("Heimat");

        Assert.Equal(FavoriteResult.StationUnknown, await _fixture.Service.AddStationAsync(group, "unbekannt"));
        Assert.Equal(FavoriteResult.GroupNotFound, await _fixture.Service.AddStationAsync(777, FavoritesFixture.StationA));
        Assert.Equal(0, (await _fixture.Service.GetGroupAsync(group))!.StationCount);
    }

    /// <summary>
    /// Prüft, dass „Anlegen und hinzufügen“ in einem Schritt die Gruppe anlegt und die Tankstelle zuordnet.
    /// </summary>
    [Fact]
    public async Task AddStationToNewGroup_CreatesGroupWithStation()
    {
        var result = await _fixture.Service.AddStationToNewGroupAsync("  Urlaub ", FavoritesFixture.StationB);

        Assert.Equal(FavoriteResult.Ok, result.Result);
        Assert.Equal(new FavoriteGroup(result.Group!.Id, "Urlaub", null, 1), result.Group);
        Assert.Equal([result.Group.Id], (await _fixture.Service.GetGroupsOfStationAsync(FavoritesFixture.StationB)).Select(g => g.Id));
    }

    /// <summary>
    /// Prüft, dass beim Anlegen und Hinzufügen bei einem Fehler weder Gruppe noch Zuordnung entstehen (Namensdoppel, ungültiger Name, unbekannte Tankstelle).
    /// </summary>
    [Fact]
    public async Task AddStationToNewGroup_Failure_StoresNothing()
    {
        await NewGroupAsync("Urlaub");

        Assert.Equal(FavoriteResult.DuplicateName, (await _fixture.Service.AddStationToNewGroupAsync("urlaub", FavoritesFixture.StationA)).Result);
        Assert.Equal(FavoriteResult.InvalidName, (await _fixture.Service.AddStationToNewGroupAsync("  ", FavoritesFixture.StationA)).Result);
        Assert.Equal(FavoriteResult.StationUnknown, (await _fixture.Service.AddStationToNewGroupAsync("Neu", "unbekannt")).Result);

        var groups = await _fixture.Service.GetGroupsAsync();
        Assert.Equal("Urlaub", Assert.Single(groups).Name);
        Assert.Equal(0, groups[0].StationCount);
    }

    /// <summary>
    /// Prüft das Entfernen aus einer einzelnen Gruppe: Die Zuordnung zu anderen Gruppen bleibt.
    /// </summary>
    [Fact]
    public async Task RemoveStation_FromOneGroup_KeepsOthers()
    {
        var work = await NewGroupAsync("Arbeitsweg");
        var home = await NewGroupAsync("Heimat");
        await _fixture.Service.AddStationAsync(work, FavoritesFixture.StationA);
        await _fixture.Service.AddStationAsync(home, FavoritesFixture.StationA);

        Assert.Equal(FavoriteResult.Ok, await _fixture.Service.RemoveStationAsync(FavoritesFixture.StationA, [work]));

        Assert.Equal([home], (await _fixture.Service.GetGroupsOfStationAsync(FavoritesFixture.StationA)).Select(g => g.Id));
    }

    /// <summary>
    /// Prüft das Entfernen aus mehreren Gruppen in einem Schritt und dass andere Tankstellen unberührt bleiben.
    /// </summary>
    [Fact]
    public async Task RemoveStation_FromSeveralGroups_RemovesAllChosen()
    {
        var work = await NewGroupAsync("Arbeitsweg");
        var home = await NewGroupAsync("Heimat");
        var trip = await NewGroupAsync("Urlaub");
        foreach (var group in new[] { work, home, trip })
        {
            await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA);
        }

        await _fixture.Service.AddStationAsync(work, FavoritesFixture.StationB);

        Assert.Equal(FavoriteResult.Ok, await _fixture.Service.RemoveStationAsync(FavoritesFixture.StationA, [work, trip]));

        Assert.Equal([home], (await _fixture.Service.GetGroupsOfStationAsync(FavoritesFixture.StationA)).Select(g => g.Id));
        Assert.Equal([work], (await _fixture.Service.GetGroupsOfStationAsync(FavoritesFixture.StationB)).Select(g => g.Id));
    }

    /// <summary>
    /// Prüft, dass das Entfernen aus Gruppen, denen die Tankstelle nicht angehört, als „nicht zugeordnet“ gemeldet wird.
    /// </summary>
    [Fact]
    public async Task RemoveStation_NotMember_ReportsNotMember()
    {
        var group = await NewGroupAsync("Heimat");

        Assert.Equal(FavoriteResult.NotMember, await _fixture.Service.RemoveStationAsync(FavoritesFixture.StationA, [group]));
        Assert.Equal(FavoriteResult.NotMember, await _fixture.Service.RemoveStationAsync(FavoritesFixture.StationA, []));
    }

    /// <summary>
    /// Prüft, dass die Zuordnungen einen Neustart überstehen.
    /// </summary>
    [Fact]
    public async Task Membership_SurvivesRestart()
    {
        var group = await NewGroupAsync("Heimat");
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationC);

        var restarted = _fixture.CreateService();

        Assert.Equal(["Heimat"], (await restarted.GetGroupsOfStationAsync(FavoritesFixture.StationC)).Select(g => g.Name));
    }
}
