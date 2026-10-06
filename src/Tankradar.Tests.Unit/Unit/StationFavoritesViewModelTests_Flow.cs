using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Karte „Favoritengruppen“ der Detailansicht: Hinzufügen zu bestehender oder neuer Gruppe, Entfernen aus einer oder mehreren Gruppen und Fehlermeldungen.
/// </summary>
public class StationFavoritesViewModelTests_Flow : BaseTest
{
    private readonly FavoritesFixture _fixture = new();
    private readonly RecordingLogger<StationFavoritesViewModel> _logger = new();

    private StationFavoritesViewModel CreateViewModel()
    {
        return new StationFavoritesViewModel(_fixture.Service, _logger);
    }

    private async Task<StationFavoritesViewModel> OpenAsync(string stationId = FavoritesFixture.StationA)
    {
        var viewModel = CreateViewModel();
        await viewModel.SetStationAsync(stationId);
        return viewModel;
    }

    private static async Task RunAsync(StationFavoritesViewModel viewModel, System.Windows.Input.ICommand command)
    {
        command.Execute(null);
        await viewModel.LastTask;
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
    /// Prüft, dass eine Tankstelle ohne Gruppen „Zu Favoriten hinzufügen“ bietet, aber kein „Aus Favoriten entfernen“.
    /// </summary>
    [Fact]
    public async Task NoGroups_OffersOnlyAdd()
    {
        var viewModel = await OpenAsync();

        Assert.False(viewModel.HasAssignedGroups);
        Assert.True(viewModel.HasNoAssignedGroups);
        Assert.False(viewModel.IsAddOpen);
    }

    /// <summary>
    /// Prüft, dass die Auswahl nur Gruppen anbietet, denen die Tankstelle noch nicht angehört, und das Antippen einer Gruppe die Tankstelle zuordnet.
    /// </summary>
    [Fact]
    public async Task Add_ToExistingGroup_AssignsAndClosesPanel()
    {
        var work = (await _fixture.Service.CreateGroupAsync("Arbeitsweg", null)).Group!.Id;
        await _fixture.Service.CreateGroupAsync("Heimat", null);
        await _fixture.Service.AddStationAsync(work, FavoritesFixture.StationA);
        var viewModel = await OpenAsync();

        await RunAsync(viewModel, viewModel.ToggleAddCommand);

        Assert.True(viewModel.IsAddOpen);
        Assert.Equal(["Heimat"], viewModel.AddOptions.Select(option => option.Name));

        viewModel.AddOptions[0].InvokeCommand.Execute(null);
        await viewModel.LastTask;

        Assert.False(viewModel.IsAddOpen);
        Assert.Equal(["Arbeitsweg", "Heimat"], viewModel.AssignedGroups.Select(group => group.Name));
        Assert.True(viewModel.HasAssignedGroups);
        Assert.Empty(viewModel.AddOptions);
        Assert.True(viewModel.HasNoAddOptions);
    }

    /// <summary>
    /// Prüft, dass über den Namen einer neuen Gruppe in einem Schritt angelegt und zugeordnet wird; „Anlegen“ ist erst mit einem Namen bedienbar.
    /// </summary>
    [Fact]
    public async Task Add_ToNewGroup_CreatesAndAssigns()
    {
        var viewModel = await OpenAsync();
        await RunAsync(viewModel, viewModel.ToggleAddCommand);
        Assert.False(viewModel.CanCreate);

        viewModel.NewGroupName = "  Urlaub ";
        Assert.True(viewModel.CanCreate);
        await RunAsync(viewModel, viewModel.CreateAndAddCommand);

        Assert.False(viewModel.IsAddOpen);
        Assert.Equal(["Urlaub"], viewModel.AssignedGroups.Select(group => group.Name));
        Assert.Equal(string.Empty, viewModel.NewGroupName);
        Assert.False(viewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein bereits vergebener Name gemeldet wird und die Auswahl offen bleibt.
    /// </summary>
    [Fact]
    public async Task Add_ToNewGroup_DuplicateName_ShowsMessageAndStaysOpen()
    {
        await _fixture.Service.CreateGroupAsync("Urlaub", null);
        var viewModel = await OpenAsync();
        await RunAsync(viewModel, viewModel.ToggleAddCommand);

        viewModel.NewGroupName = "urlaub";
        await RunAsync(viewModel, viewModel.CreateAndAddCommand);

        Assert.True(viewModel.IsAddOpen);
        Assert.Equal("Eine Gruppe mit diesem Namen gibt es bereits.", viewModel.StatusMessage);
        Assert.False(viewModel.HasAssignedGroups);
    }

    /// <summary>
    /// Prüft, dass „Abbrechen“ die Auswahl schließt und den eingegebenen Namen verwirft; ein zweites Tippen auf „Zu Favoriten hinzufügen“ schließt ebenfalls.
    /// </summary>
    [Fact]
    public async Task Cancel_ClosesPanel()
    {
        var viewModel = await OpenAsync();
        await RunAsync(viewModel, viewModel.ToggleAddCommand);
        viewModel.NewGroupName = "Entwurf";

        viewModel.CancelCommand.Execute(null);

        Assert.False(viewModel.IsAddOpen);
        Assert.Equal(string.Empty, viewModel.NewGroupName);

        await RunAsync(viewModel, viewModel.ToggleAddCommand);
        await RunAsync(viewModel, viewModel.ToggleAddCommand);
        Assert.False(viewModel.IsAddOpen);
    }

    /// <summary>
    /// Prüft, dass bei genau einer Gruppe „Aus Favoriten entfernen“ sofort entfernt.
    /// </summary>
    [Fact]
    public async Task Remove_FromSingleGroup_RemovesImmediately()
    {
        var group = (await _fixture.Service.CreateGroupAsync("Heimat", null)).Group!.Id;
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA);
        var viewModel = await OpenAsync();
        Assert.True(viewModel.HasAssignedGroups);

        await RunAsync(viewModel, viewModel.RemoveCommand);

        Assert.False(viewModel.IsRemoveOpen);
        Assert.False(viewModel.HasAssignedGroups);
        Assert.Empty(await _fixture.Service.GetGroupsOfStationAsync(FavoritesFixture.StationA));
        Assert.Equal(["Heimat"], viewModel.AddOptions.Select(option => option.Name));
    }

    /// <summary>
    /// Prüft, dass bei mehreren Gruppen eine Auswahl erscheint, „Entfernen“ erst mit Auswahl bedienbar ist und nur aus den gewählten Gruppen entfernt wird.
    /// </summary>
    [Fact]
    public async Task Remove_FromSeveralGroups_RemovesOnlyChosen()
    {
        foreach (var name in new[] { "Arbeitsweg", "Heimat", "Urlaub" })
        {
            var id = (await _fixture.Service.CreateGroupAsync(name, null)).Group!.Id;
            await _fixture.Service.AddStationAsync(id, FavoritesFixture.StationA);
        }

        var viewModel = await OpenAsync();

        await RunAsync(viewModel, viewModel.RemoveCommand);

        Assert.True(viewModel.IsRemoveOpen);
        Assert.Equal(["Arbeitsweg", "Heimat", "Urlaub"], viewModel.RemoveOptions.Select(option => option.Name));
        Assert.False(viewModel.CanConfirmRemove);

        viewModel.RemoveOptions[0].InvokeCommand.Execute(null);
        viewModel.RemoveOptions[2].InvokeCommand.Execute(null);
        Assert.True(viewModel.CanConfirmRemove);
        Assert.Equal("Ausgewählt", viewModel.RemoveOptions[0].SelectionHint);
        Assert.Equal(string.Empty, viewModel.RemoveOptions[1].SelectionHint);

        viewModel.RemoveOptions[2].InvokeCommand.Execute(null);
        Assert.False(viewModel.RemoveOptions[2].IsSelected);

        viewModel.RemoveOptions[2].InvokeCommand.Execute(null);
        await RunAsync(viewModel, viewModel.ConfirmRemoveCommand);

        Assert.False(viewModel.IsRemoveOpen);
        Assert.Equal(["Heimat"], viewModel.AssignedGroups.Select(group => group.Name));
        Assert.Equal(["Arbeitsweg", "Urlaub"], viewModel.AddOptions.Select(option => option.Name));
    }

    /// <summary>
    /// Prüft, dass der Wechsel zu einer anderen Tankstelle deren Gruppen zeigt, offene Auswahlen schließt und Meldungen löscht.
    /// </summary>
    [Fact]
    public async Task SetStation_ResetsPanelsAndLoadsOtherStationGroups()
    {
        var group = (await _fixture.Service.CreateGroupAsync("Heimat", null)).Group!.Id;
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationB);
        var viewModel = await OpenAsync();
        await RunAsync(viewModel, viewModel.ToggleAddCommand);

        await viewModel.SetStationAsync(FavoritesFixture.StationB);

        Assert.False(viewModel.IsAddOpen);
        Assert.Equal(["Heimat"], viewModel.AssignedGroups.Select(g => g.Name));
    }

    /// <summary>
    /// Prüft, dass eine Tankstelle, die lokal nicht gespeichert ist, mit verständlicher Meldung abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task Add_UnknownStation_ShowsMessage()
    {
        await _fixture.Service.CreateGroupAsync("Heimat", null);
        var viewModel = await OpenAsync("unbekannt");
        await RunAsync(viewModel, viewModel.ToggleAddCommand);

        viewModel.AddOptions[0].InvokeCommand.Execute(null);
        await viewModel.LastTask;

        Assert.Contains("lokal nicht gespeichert", viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.True(viewModel.IsAddOpen);
    }

    /// <summary>
    /// Prüft, dass ein Speicherfehler gemeldet wird, ohne Namen zu protokollieren, und die Bedienung danach wieder möglich ist.
    /// </summary>
    [Fact]
    public async Task StorageFailure_ShowsMessageWithoutLoggingNames()
    {
        var viewModel = await OpenAsync();
        await RunAsync(viewModel, viewModel.ToggleAddCommand);
        viewModel.NewGroupName = "Geheimname";
        _fixture.Factory.FailingCreations = 1;

        await RunAsync(viewModel, viewModel.CreateAndAddCommand);

        Assert.Equal("Die Änderung konnte nicht gespeichert werden.", viewModel.StatusMessage);
        Assert.DoesNotContain("Geheimname", _logger.AllText, StringComparison.Ordinal);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.CanCreate);

        await RunAsync(viewModel, viewModel.CreateAndAddCommand);
        Assert.Equal(["Geheimname"], viewModel.AssignedGroups.Select(group => group.Name));
    }

    /// <summary>
    /// Prüft, dass beim Erscheinen der Seite die Gruppen neu geladen werden (z. B. nach Umbenennen in der Gruppenansicht).
    /// </summary>
    [Fact]
    public async Task OnAppearing_ReloadsGroups()
    {
        var group = (await _fixture.Service.CreateGroupAsync("Heimat", null)).Group!.Id;
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA);
        var viewModel = await OpenAsync();
        await _fixture.Service.UpdateGroupAsync(group, "Zuhause", null);

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.Equal(["Zuhause"], viewModel.AssignedGroups.Select(g => g.Name));
    }

    /// <summary>
    /// Prüft, dass das Löschen der Gruppe in der Gruppenansicht die Zuordnung in der Detailansicht entfernt.
    /// </summary>
    [Fact]
    public async Task OnAppearing_AfterGroupDeleted_ShowsNoGroup()
    {
        var group = (await _fixture.Service.CreateGroupAsync("Heimat", null)).Group!.Id;
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA);
        var viewModel = await OpenAsync();
        await _fixture.Service.DeleteGroupAsync(group);

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.False(viewModel.HasAssignedGroups);
        Assert.Equal(FavoriteResult.NotMember, await _fixture.Service.RemoveStationAsync(FavoritesFixture.StationA, [group]));
    }
}
