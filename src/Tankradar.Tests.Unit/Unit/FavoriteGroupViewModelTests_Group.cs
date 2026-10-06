using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Gruppenansicht: Anzeige und Ordnung der Tankstellen, Umbenennen, Beschreibung, Löschen nach Rückfrage sowie Notiz und Priorität je Eintrag.
/// </summary>
public class FavoriteGroupViewModelTests_Group : BaseTest
{
    private readonly FavoritesFixture _fixture = new();
    private readonly FakeFavoriteGroupNavigator _navigator = new();
    private readonly RecordingLogger<FavoriteGroupViewModel> _logger = new();

    private FavoriteGroupViewModel CreateViewModel()
    {
        return new FavoriteGroupViewModel(_fixture.Service, _navigator, _logger);
    }

    private async Task<(FavoriteGroupViewModel ViewModel, long GroupId)> OpenGroupAsync()
    {
        var group = (await _fixture.Service.CreateGroupAsync("Arbeitsweg", "Täglicher Weg")).Group!.Id;
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationB);
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA);
        var viewModel = CreateViewModel();
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { ["groupId"] = group });
        viewModel.OnAppearing();
        await viewModel.LastTask;
        return (viewModel, group);
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
    /// Prüft, dass Name, Beschreibung und die Tankstellen der Gruppe angezeigt werden.
    /// </summary>
    [Fact]
    public async Task Load_ShowsGroupAndEntries()
    {
        var (viewModel, _) = await OpenGroupAsync();

        Assert.Equal("Arbeitsweg", viewModel.Name);
        Assert.Equal("Täglicher Weg", viewModel.Description);
        Assert.True(viewModel.HasDescription);
        Assert.True(viewModel.HasGroup);
        Assert.Equal(["Alpha Tankstelle", "Beta Tankstelle"], viewModel.Entries.Select(entry => entry.StationName));
        Assert.Equal("Hauptstraße 1, 10115 Berlin", viewModel.Entries[0].AddressText);
        Assert.True(viewModel.Entries[0].HasAddress);
        Assert.False(viewModel.Entries[1].HasAddress);
        Assert.False(viewModel.ShowEmptyHint);
        Assert.Equal("Favoritengruppe", viewModel.Title);
    }

    /// <summary>
    /// Prüft, dass eine leere Gruppe den Hinweis zeigt und eine fehlende Gruppe gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Load_EmptyOrMissingGroup()
    {
        var empty = (await _fixture.Service.CreateGroupAsync("Leer", null)).Group!.Id;
        var viewModel = CreateViewModel();
        viewModel.Show(empty);
        await viewModel.LoadAsync();
        Assert.True(viewModel.ShowEmptyHint);
        Assert.False(viewModel.HasEntries);

        viewModel.Show(9999);
        await viewModel.LoadAsync();
        Assert.False(viewModel.HasGroup);
        Assert.Equal("Die Gruppe gibt es nicht mehr.", viewModel.StatusMessage);
        Assert.False(viewModel.ShowEmptyHint);
    }

    /// <summary>
    /// Prüft, dass ohne Navigationsparameter nichts geladen wird und fremde Parametertypen ignoriert werden.
    /// </summary>
    [Fact]
    public async Task Load_WithoutGroup_DoesNothing()
    {
        var viewModel = CreateViewModel();
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { ["groupId"] = "keine Zahl" });

        await viewModel.LoadAsync();

        Assert.False(viewModel.HasGroup);
        Assert.False(viewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft das Umbenennen und Ändern der Beschreibung über das Formular.
    /// </summary>
    [Fact]
    public async Task Edit_RenamesGroupAndChangesDescription()
    {
        var (viewModel, group) = await OpenGroupAsync();

        viewModel.EditCommand.Execute(null);
        Assert.True(viewModel.IsEditing);
        Assert.False(viewModel.IsNotEditing);
        Assert.Equal("Arbeitsweg", viewModel.EditName);
        Assert.Equal("Täglicher Weg", viewModel.EditDescription);
        viewModel.EditName = "Pendeln";
        viewModel.EditDescription = string.Empty;
        viewModel.SaveEditCommand.Execute(null);
        await viewModel.LastTask;

        Assert.False(viewModel.IsEditing);
        Assert.Equal("Pendeln", viewModel.Name);
        Assert.False(viewModel.HasDescription);
        Assert.Equal("Pendeln", (await _fixture.Service.GetGroupAsync(group))!.Name);
    }

    /// <summary>
    /// Prüft, dass ein doppelter Name beim Umbenennen gemeldet wird und das Formular offen bleibt; „Abbrechen“ verwirft die Eingaben.
    /// </summary>
    [Fact]
    public async Task Edit_DuplicateName_ShowsMessageAndCancelKeepsName()
    {
        var (viewModel, _) = await OpenGroupAsync();
        await _fixture.Service.CreateGroupAsync("Heimat", null);

        viewModel.EditCommand.Execute(null);
        viewModel.EditName = "heimat";
        viewModel.SaveEditCommand.Execute(null);
        await viewModel.LastTask;

        Assert.True(viewModel.IsEditing);
        Assert.Equal("Eine Gruppe mit diesem Namen gibt es bereits.", viewModel.StatusMessage);

        viewModel.CancelEditCommand.Execute(null);
        Assert.False(viewModel.IsEditing);
        Assert.Equal("Arbeitsweg", viewModel.Name);
    }

    /// <summary>
    /// Prüft, dass das Löschen erst nach Rückfrage geschieht, „Abbrechen“ die Gruppe behält und „Ja, löschen“ sie entfernt und die Ansicht schließt.
    /// </summary>
    [Fact]
    public async Task Delete_RequiresConfirmation()
    {
        var (viewModel, group) = await OpenGroupAsync();

        viewModel.DeleteCommand.Execute(null);
        Assert.True(viewModel.IsConfirmingDelete);
        Assert.Contains("„Arbeitsweg“", viewModel.DeleteQuestion, StringComparison.Ordinal);
        Assert.Contains("2 Tankstellen", viewModel.DeleteQuestion, StringComparison.Ordinal);
        Assert.NotNull(await _fixture.Service.GetGroupAsync(group));

        viewModel.CancelDeleteCommand.Execute(null);
        Assert.False(viewModel.IsConfirmingDelete);
        Assert.NotNull(await _fixture.Service.GetGroupAsync(group));
        Assert.Equal(0, _navigator.Closed);

        viewModel.DeleteCommand.Execute(null);
        viewModel.ConfirmDeleteCommand.Execute(null);
        await viewModel.LastTask;

        Assert.Null(await _fixture.Service.GetGroupAsync(group));
        Assert.Equal(1, _navigator.Closed);
        Assert.False(viewModel.IsConfirmingDelete);
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Löschen gemeldet wird und die Ansicht geöffnet bleibt.
    /// </summary>
    [Fact]
    public async Task Delete_StorageFailure_ShowsMessage()
    {
        var (viewModel, group) = await OpenGroupAsync();
        viewModel.DeleteCommand.Execute(null);
        _fixture.Factory.FailingCreations = 1;

        viewModel.ConfirmDeleteCommand.Execute(null);
        await viewModel.LastTask;

        Assert.Equal("Die Änderung konnte nicht gespeichert werden.", viewModel.StatusMessage);
        Assert.Equal(0, _navigator.Closed);
        Assert.NotNull(await _fixture.Service.GetGroupAsync(group));
    }

    /// <summary>
    /// Prüft das Pflegen von Notiz und Priorität: Das Formular übernimmt die gespeicherten Werte, die Liste ordnet nach Priorität neu.
    /// </summary>
    [Fact]
    public async Task EditEntry_SavesNoteAndPriorityAndReorders()
    {
        var (viewModel, _) = await OpenGroupAsync();
        var beta = viewModel.Entries[1];
        Assert.Equal("Beta Tankstelle", beta.StationName);

        beta.EditCommand.Execute(null);
        Assert.True(beta.IsEditing);
        Assert.Equal(FavoritePriority.None, beta.SelectedPriority);
        beta.EditNote = "Immer günstig";
        beta.PriorityOptions.Single(option => option.Value == FavoritePriority.High).SelectCommand.Execute(null);
        Assert.Equal(FavoritePriority.High, beta.SelectedPriority);
        Assert.Single(beta.PriorityOptions, option => option.IsSelected);
        beta.SaveCommand.Execute(null);
        await beta.LastSaveTask;

        Assert.Equal(["Beta Tankstelle", "Alpha Tankstelle"], viewModel.Entries.Select(entry => entry.StationName));
        var saved = viewModel.Entries[0];
        Assert.Equal("Immer günstig", saved.Note);
        Assert.True(saved.HasNote);
        Assert.Equal("Priorität: Hoch", saved.PriorityText);
        Assert.False(saved.IsEditing);
        Assert.False(viewModel.Entries[1].HasNote);
    }

    /// <summary>
    /// Prüft, dass das Formular eines Eintrags die gespeicherte Priorität vorwählt und „Abbrechen“ nichts ändert.
    /// </summary>
    [Fact]
    public async Task EditEntry_PreselectsStoredValuesAndCancelKeeps()
    {
        var (viewModel, group) = await OpenGroupAsync();
        await _fixture.Service.UpdateEntryAsync(group, FavoritesFixture.StationA, "Notiz", FavoritePriority.Medium);
        await viewModel.LoadAsync();
        var alpha = viewModel.Entries[0];

        alpha.EditCommand.Execute(null);

        Assert.Equal("Notiz", alpha.EditNote);
        Assert.Equal(FavoritePriority.Medium, alpha.SelectedPriority);

        alpha.EditNote = "geändert";
        alpha.CancelCommand.Execute(null);
        Assert.False(alpha.IsEditing);
        Assert.Equal("Notiz", alpha.Note);
        Assert.Equal("Notiz", (await _fixture.Service.GetEntriesAsync(group))[0].Note);
    }

    /// <summary>
    /// Prüft, dass eine zu lange Notiz gemeldet wird und das Formular offen bleibt.
    /// </summary>
    [Fact]
    public async Task EditEntry_TooLongNote_ShowsMessage()
    {
        var (viewModel, _) = await OpenGroupAsync();
        var alpha = viewModel.Entries[0];

        alpha.EditCommand.Execute(null);
        alpha.EditNote = new string('n', FavoriteLimits.MaxNoteLength + 1);
        alpha.SaveCommand.Execute(null);
        await alpha.LastSaveTask;

        Assert.Contains("zu lang", viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.True(alpha.IsEditing);
    }

    /// <summary>
    /// Prüft, dass ein Ladefehler gemeldet wird, ohne den Namen der Gruppe zu protokollieren.
    /// </summary>
    [Fact]
    public async Task Load_StorageFailure_ShowsMessageWithoutLoggingNames()
    {
        var (viewModel, _) = await OpenGroupAsync();
        _fixture.Factory.FailingCreations = 1;

        await viewModel.LoadAsync();

        Assert.Equal("Die Favoriten konnten nicht geladen werden.", viewModel.StatusMessage);
        Assert.DoesNotContain("Arbeitsweg", _logger.AllText, StringComparison.Ordinal);
    }
}
