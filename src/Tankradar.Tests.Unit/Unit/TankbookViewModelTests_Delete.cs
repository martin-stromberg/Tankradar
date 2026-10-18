using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Löschen im <see cref="TankbookViewModel"/>: Rückfrage im Eintrag (schlicht oder mit ausdrücklichem Mitlöschen der Vorgänge), Abbrechen und Speicherfehler.
/// </summary>
public class TankbookViewModelTests_Delete : BaseTest
{
    private readonly VehicleFixture _fixture = new();
    private readonly RecordingLogger<TankbookViewModel> _logger = new();
    private readonly FakeScreenReaderAnnouncer _announcer = new();

    private async Task<TankbookViewModel> OpenAsync()
    {
        var viewModel = new TankbookViewModel(_fixture.Service, new FakeTankbookNavigator(), _logger, _announcer);
        viewModel.OnAppearing();
        await viewModel.LastTask;
        return viewModel;
    }

    private async Task<long> CreateAsync(string name)
    {
        return (await _fixture.Service.CreateVehicleAsync(new VehicleInput(name, VehicleFuelKind.Diesel, 1000, null))).Vehicle!.Id;
    }

    private static async Task PressAsync(TankbookViewModel viewModel, System.Windows.Input.ICommand command)
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
    /// Prüft die schlichte Rückfrage ohne Vorgänge und das Löschen nach „Ja, löschen“.
    /// </summary>
    [Fact]
    public async Task Delete_WithoutEntries_AsksSimpleQuestion()
    {
        await CreateAsync("Golf");
        var viewModel = await OpenAsync();
        var item = viewModel.Vehicles[0];

        await PressAsync(viewModel, item.DeleteCommand);

        Assert.True(item.IsConfirmingDelete);
        Assert.Equal("Fahrzeug „Golf“ löschen?", item.DeleteQuestion);
        Assert.Equal("Ja, löschen", item.ConfirmDeleteLabel);

        await PressAsync(viewModel, item.ConfirmDeleteCommand);

        Assert.Empty(viewModel.Vehicles);
        Assert.True(viewModel.ShowEmptyHint);
    }

    /// <summary>
    /// Prüft die ausdrückliche Rückfrage bei vorhandenen Vorgängen und das Mitlöschen nach der Bestätigung.
    /// </summary>
    [Fact]
    public async Task Delete_WithEntries_AsksExplicitlyAndDeletesEntries()
    {
        var golf = await CreateAsync("Golf");
        var polo = await CreateAsync("Polo");
        await _fixture.AddEntriesAsync(golf, 2);
        await _fixture.AddEntriesAsync(polo, 1);
        var viewModel = await OpenAsync();
        var item = viewModel.Vehicles.Single(vehicle => vehicle.Name == "Golf");

        await PressAsync(viewModel, item.DeleteCommand);

        Assert.Contains("2 zugehörigen Tank- und Ladevorgänge endgültig gelöscht", item.DeleteQuestion, StringComparison.Ordinal);
        Assert.Contains("Erstelle vorher in den Optionen eine Sicherung", item.DeleteQuestion, StringComparison.Ordinal);
        Assert.Equal("Fahrzeug und Vorgänge löschen", item.ConfirmDeleteLabel);

        await PressAsync(viewModel, item.ConfirmDeleteCommand);

        Assert.Equal(["Polo"], viewModel.Vehicles.Select(vehicle => vehicle.Name));
        Assert.Equal(0, await _fixture.Service.CountEntriesAsync(golf));
        Assert.Equal(1, await _fixture.CountAllEntriesAsync());
    }

    /// <summary>
    /// Prüft, dass „Abbrechen“ die Rückfrage schließt und das Fahrzeug behält.
    /// </summary>
    [Fact]
    public async Task Delete_Cancel_KeepsVehicle()
    {
        await CreateAsync("Golf");
        var viewModel = await OpenAsync();
        var item = viewModel.Vehicles[0];
        await PressAsync(viewModel, item.DeleteCommand);

        item.CancelDeleteCommand.Execute(null);

        Assert.False(item.IsConfirmingDelete);
        Assert.Single(await _fixture.Service.GetVehiclesAsync());
        Assert.Single(viewModel.Vehicles);
    }

    /// <summary>
    /// Prüft, dass ein Speicherfehler beim Löschen gemeldet wird und das Fahrzeug bleibt.
    /// </summary>
    [Fact]
    public async Task Delete_StorageFailure_ShowsMessageAndKeepsVehicle()
    {
        await CreateAsync("Geheimauto");
        var viewModel = await OpenAsync();
        var item = viewModel.Vehicles[0];
        await PressAsync(viewModel, item.DeleteCommand);
        _fixture.Factory.FailingCreations = 1;

        await PressAsync(viewModel, item.ConfirmDeleteCommand);

        Assert.Equal("Die Änderung konnte nicht gespeichert werden.", viewModel.StatusMessage);
        Assert.Single(await _fixture.Service.GetVehiclesAsync());
        Assert.False(viewModel.IsBusy);
        Assert.DoesNotContain("Geheimauto", _logger.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass „Löschen“ ein offenes Formular schließt.
    /// </summary>
    [Fact]
    public async Task Delete_ClosesOpenForm()
    {
        await CreateAsync("Golf");
        var viewModel = await OpenAsync();
        viewModel.AddVehicleCommand.Execute(null);

        await PressAsync(viewModel, viewModel.Vehicles[0].DeleteCommand);

        Assert.False(viewModel.IsFormOpen);
        Assert.True(viewModel.Vehicles[0].IsConfirmingDelete);
    }

    /// <summary>
    /// Prüft, dass die Rückfrage eines zweiten Fahrzeugs die erste schließt.
    /// </summary>
    [Fact]
    public async Task Delete_SecondVehicle_ClosesFirstConfirmation()
    {
        await CreateAsync("Golf");
        await CreateAsync("Polo");
        var viewModel = await OpenAsync();
        var golf = viewModel.Vehicles[0];
        var polo = viewModel.Vehicles[1];
        await PressAsync(viewModel, golf.DeleteCommand);

        await PressAsync(viewModel, polo.DeleteCommand);

        Assert.False(golf.IsConfirmingDelete);
        Assert.True(polo.IsConfirmingDelete);
    }

    /// <summary>
    /// Prüft, dass „Bearbeiten“ eine offene Rückfrage schließt.
    /// </summary>
    [Fact]
    public async Task Edit_ClosesOpenConfirmation()
    {
        await CreateAsync("Golf");
        var viewModel = await OpenAsync();
        var golf = viewModel.Vehicles[0];
        await PressAsync(viewModel, golf.DeleteCommand);

        golf.EditCommand.Execute(null);

        Assert.True(viewModel.IsFormOpen);
        Assert.False(golf.IsConfirmingDelete);
    }

    /// <summary>
    /// Prüft, dass das Bestätigen bei einem inzwischen gelöschten Fahrzeug die Rückfrage schließt und die Liste neu lädt.
    /// </summary>
    [Fact]
    public async Task Delete_VehicleDeletedMeanwhile_ClosesConfirmationAndReloads()
    {
        var id = await CreateAsync("Golf");
        var viewModel = await OpenAsync();
        var item = viewModel.Vehicles[0];
        await PressAsync(viewModel, item.DeleteCommand);
        await _fixture.Service.DeleteVehicleAsync(id, 0);

        await PressAsync(viewModel, item.ConfirmDeleteCommand);

        Assert.False(item.IsConfirmingDelete);
        Assert.False(viewModel.HasStatusMessage);
        Assert.Empty(viewModel.Vehicles);
        Assert.True(viewModel.ShowEmptyHint);
    }

    /// <summary>
    /// Prüft, dass die erscheinende Rückfrage angesagt wird, die Schaltflächen der Rückfrage ihren sichtbaren Text als Namen behalten (WCAG 2.5.3)
    /// und der Hinweis den Fahrzeugnamen und die Zahl der Vorgänge nennt.
    /// </summary>
    [Fact]
    public async Task Delete_ButtonsNameIsVisibleTextAndHintNamesVehicle()
    {
        var golf = await CreateAsync("Golf");
        await _fixture.AddEntriesAsync(golf, 2);
        var viewModel = await OpenAsync();
        var item = viewModel.Vehicles[0];

        await PressAsync(viewModel, item.DeleteCommand);

        Assert.Equal([item.DeleteQuestion], _announcer.Announcements);
        Assert.Equal("Fahrzeug „Golf“", item.VehicleHint);
        Assert.Equal("Fahrzeug und Vorgänge löschen", item.ConfirmDeleteLabel);
        Assert.Equal("Fahrzeug „Golf“ und 2 Tank- und Ladevorgänge endgültig löschen", item.ConfirmDeleteHint);
        Assert.Equal("Fahrzeug „Golf“ behalten", item.CancelDeleteHint);
        Assert.Equal(2, item.ShownEntryCount);
    }

    /// <summary>
    /// Prüft die Neuzählung beim Bestätigen: Ist inzwischen ein Vorgang hinzugekommen, bleibt das Fahrzeug, die Rückfrage nennt die neue Zahl und wird angesagt;
    /// erst die erneute Bestätigung löscht.
    /// </summary>
    [Fact]
    public async Task Delete_EntryAddedMeanwhile_ShowsUpdatedQuestionAndKeepsVehicle()
    {
        var golf = await CreateAsync("Golf");
        await _fixture.AddEntriesAsync(golf, 2);
        var viewModel = await OpenAsync();
        var item = viewModel.Vehicles[0];
        await PressAsync(viewModel, item.DeleteCommand);
        await _fixture.AddEntriesAsync(golf, 1, odometerKm: 5000);

        await PressAsync(viewModel, item.ConfirmDeleteCommand);

        Assert.True(item.IsConfirmingDelete);
        Assert.Contains("3 zugehörigen Tank- und Ladevorgänge", item.DeleteQuestion, StringComparison.Ordinal);
        Assert.Equal("Fahrzeug „Golf“ und 3 Tank- und Ladevorgänge endgültig löschen", item.ConfirmDeleteHint);
        Assert.Equal(item.DeleteQuestion, _announcer.Announcements[^1]);
        Assert.Single(await _fixture.Service.GetVehiclesAsync());
        Assert.Equal(3, await _fixture.Service.CountEntriesAsync(golf));

        await PressAsync(viewModel, item.ConfirmDeleteCommand);

        Assert.Empty(viewModel.Vehicles);
        Assert.Equal(0, await _fixture.CountAllEntriesAsync());
    }
}
