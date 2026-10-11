using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Löschen auf der Seite <see cref="MAUI.ViewModels.TankbookEntriesViewModel"/>: Rückfrage im Eintrag, Abbrechen und WCAG-Muster der Schaltflächen.
/// </summary>
public class TankbookEntriesViewModelTests_Delete : TankbookEntriesViewModelTestBase
{
    private async Task<long> CreateVehicleWithEntryAsync()
    {
        var vehicle = await CreateVehicleAsync();
        await Fixture.CreateEntryAsync(vehicle.Id, new TankbookEntryInput(new DateTime(2026, 5, 18, 14, 30, 0), TankbookEntryKind.Fuel, 40m, 1.859m, 74.36m, 10_500, false, "Alpha Tankstelle", null, null, null));
        return vehicle.Id;
    }

    /// <summary>
    /// Prüft, dass „Löschen“ die Rückfrage zeigt und ansagt und erst „Ja, löschen“ den Vorgang entfernt.
    /// </summary>
    [Fact]
    public async Task Delete_AsksAndDeletesAfterConfirmation()
    {
        var vehicleId = await CreateVehicleWithEntryAsync();
        var viewModel = await OpenAsync(vehicleId);
        var item = viewModel.Entries[0];

        await PressAsync(viewModel, item.DeleteCommand);

        Assert.True(item.IsConfirmingDelete);
        Assert.Equal("Vorgang vom 18.05.2026, 14:30 (Alpha Tankstelle) löschen? Das lässt sich nicht rückgängig machen.", item.DeleteQuestion);
        Assert.Equal([item.DeleteQuestion], Announcer.Announcements);
        Assert.Single(await Fixture.Service.GetEntriesAsync(vehicleId));

        await PressAsync(viewModel, item.ConfirmDeleteCommand);

        Assert.Empty(viewModel.Entries);
        Assert.True(viewModel.ShowEmptyHint);
        Assert.Empty(await Fixture.Service.GetEntriesAsync(vehicleId));
    }

    /// <summary>
    /// Prüft, dass „Abbrechen“ die Rückfrage schließt und den Vorgang behält.
    /// </summary>
    [Fact]
    public async Task Delete_Cancel_KeepsEntry()
    {
        var vehicleId = await CreateVehicleWithEntryAsync();
        var viewModel = await OpenAsync(vehicleId);
        var item = viewModel.Entries[0];
        await PressAsync(viewModel, item.DeleteCommand);

        item.CancelDeleteCommand.Execute(null);

        Assert.False(item.IsConfirmingDelete);
        Assert.Single(await Fixture.Service.GetEntriesAsync(vehicleId));
    }

    /// <summary>
    /// Prüft das WCAG-Muster: Die Schaltflächen setzen keine abweichende Beschreibung, der Kontext steht in den Hinweisen.
    /// </summary>
    [Fact]
    public async Task Buttons_NameIsVisibleTextAndHintGivesContext()
    {
        var vehicleId = await CreateVehicleWithEntryAsync();
        var viewModel = await OpenAsync(vehicleId);
        var item = viewModel.Entries[0];

        Assert.Equal("Vorgang vom 18.05.2026, 14:30, Alpha Tankstelle", item.EntryHint);
        Assert.Equal("Vorgang vom 18.05.2026, 14:30, Alpha Tankstelle endgültig löschen", item.ConfirmDeleteHint);
        Assert.Equal("Vorgang behalten", item.CancelDeleteHint);
        Assert.Equal("Fahrzeug „Golf“", viewModel.AddEntryHint);
    }
}
