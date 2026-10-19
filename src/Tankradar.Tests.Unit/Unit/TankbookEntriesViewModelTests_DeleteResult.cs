using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Fehlerzweig des Löschens auf der Seite <see cref="TankbookEntriesViewModel"/>: ein Ergebnis ungleich „Ok“ und „Vorgang nicht gefunden“.
/// </summary>
public class TankbookEntriesViewModelTests_DeleteResult : TankbookEntriesViewModelTestBase
{
    /// <summary>
    /// Prüft, dass ein anderes Ergebnis als Ok oder EntryNotFound die Meldung zeigt und ansagt, ohne neu zu laden; der Vorgang bleibt.
    /// </summary>
    [Fact]
    public async Task Delete_ResultOtherThanOkOrNotFound_ShowsResultMessageAndKeepsEntries()
    {
        var vehicle = await CreateVehicleAsync();
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-1), 10_500, 40m));
        var stub = new StubTankbookEntryService(Fixture.Service) { DeleteResult = TankbookEntryResult.InvalidQuantity };
        var viewModel = new TankbookEntriesViewModel(
            Fixture.Vehicles,
            stub,
            Fixture.Photos,
            Fixture.CreateForm(Picker, Announcer, FormLogger),
            Fixture.CreateStatistics(Announcer, StatisticsLogger),
            Fixture.CreateExport(Sharer, Announcer, ExportLogger),
            Fixture.Restore,
            Navigator,
            Announcer,
            Logger);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [ShellTankbookNavigator.VehicleIdParameter] = vehicle.Id });
        viewModel.OnAppearing();
        await viewModel.LastTask;
        var item = viewModel.Entries[0];
        await PressAsync(viewModel, item.DeleteCommand);
        var loadsBefore = stub.GetEntriesCalls;
        Announcer.Announcements.Clear();

        await PressAsync(viewModel, item.ConfirmDeleteCommand);

        var expected = TankbookEntryTexts.GetResultMessage(TankbookEntryResult.InvalidQuantity, null);
        Assert.Equal(expected, viewModel.StatusMessage);
        Assert.Equal([expected], Announcer.Announcements);
        Assert.Equal(loadsBefore, stub.GetEntriesCalls);
        Assert.Single(viewModel.Entries);
        Assert.Single(await Fixture.Service.GetEntriesAsync(vehicle.Id));
        Assert.False(viewModel.IsDeleting);
    }
}
