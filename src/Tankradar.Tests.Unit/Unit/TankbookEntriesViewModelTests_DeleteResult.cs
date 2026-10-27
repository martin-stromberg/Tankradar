using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
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
        var viewModel = await OpenAsync(vehicle.Id, stub);
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
