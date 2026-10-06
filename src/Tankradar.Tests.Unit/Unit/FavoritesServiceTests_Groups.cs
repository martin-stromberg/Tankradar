using Tankradar.MAUI.Models.Favorites;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Gruppenoperationen des Favoritendienstes: Anlegen, Umbenennen, Beschreibung, Löschen, Validierung und Eindeutigkeit der Namen.
/// </summary>
public class FavoritesServiceTests_Groups : BaseTest
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
    /// Prüft, dass eine Gruppe mit getrimmtem Namen und optionaler Beschreibung angelegt und wieder geliefert wird.
    /// </summary>
    [Fact]
    public async Task CreateGroup_StoresTrimmedNameAndDescription()
    {
        var result = await _fixture.Service.CreateGroupAsync("  Arbeitsweg  ", "  Täglich  ");

        Assert.Equal(FavoriteResult.Ok, result.Result);
        Assert.Equal("Arbeitsweg", result.Group!.Name);
        var stored = await _fixture.Service.GetGroupAsync(result.Group.Id);
        Assert.Equal(new FavoriteGroup(result.Group.Id, "Arbeitsweg", "Täglich", 0), stored);
    }

    /// <summary>
    /// Prüft, dass leere oder zu lange Namen sowie zu lange Beschreibungen abgelehnt werden und nichts gespeichert wird.
    /// </summary>
    [Fact]
    public async Task CreateGroup_InvalidInput_IsRejected()
    {
        Assert.Equal(FavoriteResult.InvalidName, (await _fixture.Service.CreateGroupAsync("   ", null)).Result);
        Assert.Equal(FavoriteResult.InvalidName, (await _fixture.Service.CreateGroupAsync(new string('x', FavoriteLimits.MaxNameLength + 1), null)).Result);
        Assert.Equal(FavoriteResult.InvalidText, (await _fixture.Service.CreateGroupAsync("Heimat", new string('y', FavoriteLimits.MaxDescriptionLength + 1))).Result);
        Assert.Equal(FavoriteResult.Ok, (await _fixture.Service.CreateGroupAsync(new string('x', FavoriteLimits.MaxNameLength), new string('y', FavoriteLimits.MaxDescriptionLength))).Result);

        Assert.Single(await _fixture.Service.GetGroupsAsync());
    }

    /// <summary>
    /// Prüft, dass ein Name ohne Beachtung der Groß- und Kleinschreibung nur einmal vergeben werden kann.
    /// </summary>
    /// <param name="second">Der zweite Name, der abgelehnt werden muss.</param>
    [Theory]
    [InlineData("Urlaub")]
    [InlineData("urlaub")]
    [InlineData("  URLAUB ")]
    public async Task CreateGroup_DuplicateName_IsRejected(string second)
    {
        await _fixture.Service.CreateGroupAsync("Urlaub", null);

        var result = await _fixture.Service.CreateGroupAsync(second, null);

        Assert.Equal(FavoriteResult.DuplicateName, result.Result);
        Assert.Null(result.Group);
        Assert.Single(await _fixture.Service.GetGroupsAsync());
    }

    /// <summary>
    /// Prüft, dass Umlaute bei der Eindeutigkeit gleich behandelt werden (Ä gleich ä).
    /// </summary>
    [Fact]
    public async Task CreateGroup_DuplicateWithUmlaut_IsRejected()
    {
        await _fixture.Service.CreateGroupAsync("Ärzte", null);

        Assert.Equal(FavoriteResult.DuplicateName, (await _fixture.Service.CreateGroupAsync("ärzte", null)).Result);
    }

    /// <summary>
    /// Prüft, dass Gruppen nach Namen geordnet geliefert werden (ohne Beachtung der Groß- und Kleinschreibung, Umlaute an alphabetischer Stelle).
    /// </summary>
    [Fact]
    public async Task GetGroups_AreOrderedByName()
    {
        foreach (var name in new[] { "urlaub", "Zuhause", "Ärzte", "Arbeitsweg" })
        {
            await _fixture.Service.CreateGroupAsync(name, null);
        }

        var names = (await _fixture.Service.GetGroupsAsync()).Select(group => group.Name).ToList();

        Assert.Equal(["Arbeitsweg", "Ärzte", "urlaub", "Zuhause"], names);
    }

    /// <summary>
    /// Prüft das Umbenennen und Ändern der Beschreibung samt Entfernen der Beschreibung durch leeren Text.
    /// </summary>
    [Fact]
    public async Task UpdateGroup_RenamesAndChangesDescription()
    {
        var id = (await _fixture.Service.CreateGroupAsync("Heimat", "alt")).Group!.Id;

        Assert.Equal(FavoriteResult.Ok, await _fixture.Service.UpdateGroupAsync(id, "Zuhause", "neu"));
        Assert.Equal(new FavoriteGroup(id, "Zuhause", "neu", 0), await _fixture.Service.GetGroupAsync(id));

        Assert.Equal(FavoriteResult.Ok, await _fixture.Service.UpdateGroupAsync(id, "Zuhause", "  "));
        Assert.Null((await _fixture.Service.GetGroupAsync(id))!.Description);
    }

    /// <summary>
    /// Prüft, dass der eigene Name (auch mit geänderter Schreibweise) beim Umbenennen erlaubt ist, der Name einer anderen Gruppe aber nicht.
    /// </summary>
    [Fact]
    public async Task UpdateGroup_NameOfOtherGroup_IsRejectedOwnNameIsAllowed()
    {
        var first = (await _fixture.Service.CreateGroupAsync("Heimat", null)).Group!.Id;
        await _fixture.Service.CreateGroupAsync("Urlaub", null);

        Assert.Equal(FavoriteResult.DuplicateName, await _fixture.Service.UpdateGroupAsync(first, "urlaub", null));
        Assert.Equal(FavoriteResult.Ok, await _fixture.Service.UpdateGroupAsync(first, "HEIMAT", null));
        Assert.Equal("HEIMAT", (await _fixture.Service.GetGroupAsync(first))!.Name);
    }

    /// <summary>
    /// Prüft die Fehlerfälle beim Ändern: ungültiger Name, zu lange Beschreibung, unbekannte Gruppe.
    /// </summary>
    [Fact]
    public async Task UpdateGroup_InvalidOrUnknown_IsRejected()
    {
        var id = (await _fixture.Service.CreateGroupAsync("Heimat", null)).Group!.Id;

        Assert.Equal(FavoriteResult.InvalidName, await _fixture.Service.UpdateGroupAsync(id, " ", null));
        Assert.Equal(FavoriteResult.InvalidText, await _fixture.Service.UpdateGroupAsync(id, "Heimat", new string('z', FavoriteLimits.MaxDescriptionLength + 1)));
        Assert.Equal(FavoriteResult.GroupNotFound, await _fixture.Service.UpdateGroupAsync(9999, "Heimat", null));
        Assert.Equal("Heimat", (await _fixture.Service.GetGroupAsync(id))!.Name);
    }

    /// <summary>
    /// Prüft, dass beim Löschen einer Gruppe nur ihre Zuordnungen entfallen: Die Tankstelle bleibt bekannt und in anderen Gruppen erhalten.
    /// </summary>
    [Fact]
    public async Task DeleteGroup_RemovesOnlyItsEntries()
    {
        var work = (await _fixture.Service.CreateGroupAsync("Arbeitsweg", null)).Group!.Id;
        var home = (await _fixture.Service.CreateGroupAsync("Heimat", null)).Group!.Id;
        await _fixture.Service.AddStationAsync(work, FavoritesFixture.StationA);
        await _fixture.Service.AddStationAsync(home, FavoritesFixture.StationA);

        Assert.Equal(FavoriteResult.Ok, await _fixture.Service.DeleteGroupAsync(work));

        Assert.Null(await _fixture.Service.GetGroupAsync(work));
        Assert.Equal([home], (await _fixture.Service.GetGroupsOfStationAsync(FavoritesFixture.StationA)).Select(group => group.Id));
        await using var context = _fixture.Factory.CreateDbContext();
        Assert.Equal(3, context.Stations.Count());
    }

    /// <summary>
    /// Prüft, dass das Löschen einer unbekannten Gruppe gemeldet wird.
    /// </summary>
    [Fact]
    public async Task DeleteGroup_Unknown_ReportsNotFound()
    {
        Assert.Equal(FavoriteResult.GroupNotFound, await _fixture.Service.DeleteGroupAsync(42));
    }

    /// <summary>
    /// Prüft, dass gelöschte Namen wieder vergeben werden können.
    /// </summary>
    [Fact]
    public async Task DeleteGroup_FreesName()
    {
        var id = (await _fixture.Service.CreateGroupAsync("Heimat", null)).Group!.Id;
        await _fixture.Service.DeleteGroupAsync(id);

        Assert.Equal(FavoriteResult.Ok, (await _fixture.Service.CreateGroupAsync("Heimat", null)).Result);
    }

    /// <summary>
    /// Prüft, dass die Gruppen nach einem Neustart (neuer Dienst auf derselben Datenbank) vorhanden sind und ohne Netzverbindung verfügbar bleiben.
    /// </summary>
    [Fact]
    public async Task Groups_SurviveRestart()
    {
        await _fixture.Service.CreateGroupAsync("Urlaub", "Italien");

        var restarted = _fixture.CreateService();

        Assert.Equal("Urlaub", Assert.Single(await restarted.GetGroupsAsync()).Name);
    }
}
