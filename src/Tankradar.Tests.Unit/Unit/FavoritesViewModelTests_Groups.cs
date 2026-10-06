using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Bereich „Favoriten“: Gruppenliste, Anlegen einer Gruppe, Öffnen der Gruppenansicht und Fehlerfälle.
/// </summary>
public class FavoritesViewModelTests_Groups : BaseTest
{
    private readonly FavoritesFixture _fixture = new();
    private readonly FakeFavoriteGroupNavigator _navigator = new();
    private readonly RecordingLogger<FavoritesViewModel> _logger = new();

    private FavoritesViewModel CreateViewModel()
    {
        return new FavoritesViewModel(_fixture.Service, _navigator, _logger);
    }

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
    /// Prüft, dass ohne Gruppen der Hinweis erscheint, nicht aber vor dem Laden.
    /// </summary>
    [Fact]
    public async Task Load_WithoutGroups_ShowsEmptyHintOnlyAfterLoading()
    {
        var viewModel = CreateViewModel();
        Assert.False(viewModel.ShowEmptyHint);

        await viewModel.LoadAsync();

        Assert.True(viewModel.ShowEmptyHint);
        Assert.False(viewModel.HasGroups);
        Assert.Equal("Favoriten", viewModel.Title);
    }

    /// <summary>
    /// Prüft, dass Gruppen mit Beschreibung und Anzahl nach Namen geordnet erscheinen und das Antippen die Gruppenansicht öffnet.
    /// </summary>
    [Fact]
    public async Task Load_ShowsGroupsAndOpensSelectedGroup()
    {
        var home = (await _fixture.Service.CreateGroupAsync("Heimat", "Zuhause und Umgebung")).Group!.Id;
        var work = (await _fixture.Service.CreateGroupAsync("Arbeitsweg", null)).Group!.Id;
        await _fixture.Service.AddStationAsync(home, FavoritesFixture.StationA);
        await _fixture.Service.AddStationAsync(home, FavoritesFixture.StationB);
        var viewModel = CreateViewModel();

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.Equal(["Arbeitsweg", "Heimat"], viewModel.Groups.Select(group => group.Name));
        Assert.Equal(["0 Tankstellen", "2 Tankstellen"], viewModel.Groups.Select(group => group.CountText));
        Assert.False(viewModel.Groups[0].HasDescription);
        Assert.Equal("Zuhause und Umgebung", viewModel.Groups[1].Description);
        Assert.False(viewModel.ShowEmptyHint);

        viewModel.Groups[1].OpenCommand.Execute(null);
        await viewModel.LastTask;

        Assert.Equal([home], _navigator.Opened);
        Assert.NotEqual(work, home);
    }

    /// <summary>
    /// Prüft, dass eine neue Gruppe mit Beschreibung angelegt wird, das Formular sich schließt und die Liste aktualisiert ist.
    /// </summary>
    [Fact]
    public async Task Create_AddsGroupAndClosesForm()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        viewModel.ShowCreateCommand.Execute(null);
        Assert.True(viewModel.IsCreating);
        Assert.False(viewModel.IsNotCreating);
        viewModel.NewName = "Urlaub";
        viewModel.NewDescription = "Sommer";
        viewModel.CreateCommand.Execute(null);
        await viewModel.LastTask;

        Assert.False(viewModel.IsCreating);
        Assert.Equal("Urlaub", Assert.Single(viewModel.Groups).Name);
        Assert.Equal("Sommer", viewModel.Groups[0].Description);
        Assert.False(viewModel.ShowEmptyHint);
    }

    /// <summary>
    /// Prüft, dass ungültige und doppelte Namen gemeldet werden, das Formular offen bleibt und „Abbrechen“ es schließt.
    /// </summary>
    [Fact]
    public async Task Create_InvalidOrDuplicate_ShowsMessage()
    {
        await _fixture.Service.CreateGroupAsync("Urlaub", null);
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        viewModel.ShowCreateCommand.Execute(null);

        viewModel.NewName = "   ";
        viewModel.CreateCommand.Execute(null);
        await viewModel.LastTask;
        Assert.Contains("Namen", viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.True(viewModel.IsCreating);

        viewModel.NewName = "URLAUB";
        viewModel.CreateCommand.Execute(null);
        await viewModel.LastTask;
        Assert.Equal("Eine Gruppe mit diesem Namen gibt es bereits.", viewModel.StatusMessage);
        Assert.True(viewModel.HasStatusMessage);

        viewModel.CancelCreateCommand.Execute(null);
        Assert.False(viewModel.IsCreating);
        Assert.False(viewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Laden gemeldet wird, ohne Ausnahmetexte zu protokollieren.
    /// </summary>
    [Fact]
    public async Task Load_StorageFailure_ShowsMessage()
    {
        _fixture.Factory.FailingCreations = 1;
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync();

        Assert.Equal("Die Favoriten konnten nicht geladen werden.", viewModel.StatusMessage);
        Assert.DoesNotContain("simuliert", _logger.AllText, StringComparison.Ordinal);
        Assert.False(viewModel.ShowEmptyHint);
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Anlegen gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Create_StorageFailure_ShowsMessage()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        viewModel.ShowCreateCommand.Execute(null);
        viewModel.NewName = "Heimat";
        _fixture.Factory.FailingCreations = 1;

        viewModel.CreateCommand.Execute(null);
        await viewModel.LastTask;

        Assert.Equal("Die Änderung konnte nicht gespeichert werden.", viewModel.StatusMessage);
        Assert.True(viewModel.IsCreating);
    }
}
