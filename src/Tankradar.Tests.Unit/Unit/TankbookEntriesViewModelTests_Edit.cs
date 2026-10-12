using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Korrigieren auf der Seite <see cref="MAUI.ViewModels.TankbookEntriesViewModel"/>: vorbelegtes Formular, Speichern und neu berechneter Verbrauch.
/// </summary>
public class TankbookEntriesViewModelTests_Edit : TankbookEntriesViewModelTestBase
{
    /// <summary>
    /// Prüft, dass „Bearbeiten“ das Formular mit den gespeicherten Werten (deutsch formatiert) öffnet und das Speichern den Verbrauch neu berechnet.
    /// </summary>
    [Fact]
    public async Task Edit_PrefillsForm()
    {
        var vehicle = await CreateVehicleAsync();
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-2), 10_500, 40m));
        await Fixture.CreateEntryAsync(vehicle.Id, new TankbookEntryInput(Fixture.Now.AddDays(-1), TankbookEntryKind.Fuel, 30m, 1.859m, 55.77m, 11_000, true, "Alpha Tankstelle", null, "Notiz", null));
        var viewModel = await OpenAsync(vehicle.Id);
        var item = viewModel.Entries[0];

        item.EditCommand.Execute(null);

        var form = viewModel.Form;
        Assert.True(form.IsOpen);
        Assert.Equal("Vorgang bearbeiten", form.FormTitle);
        Assert.Equal(item.Id, form.EditingEntryId);
        Assert.Equal("30,00", form.Quantity);
        Assert.Equal("1,859", form.UnitPrice);
        Assert.Equal("55,77", form.TotalAmount);
        Assert.Equal("11000", form.Odometer);
        Assert.True(form.IsPartial);
        Assert.Equal("Notiz", form.Note);
        Assert.Equal(Fixture.Now.AddDays(-1).Date, form.EntryDate);
        Assert.False(viewModel.Entries[0].IsConfirmingDelete);

        form.IsPartial = false;
        form.Odometer = "11200";
        await PressFormAsync(viewModel, form.SaveCommand);

        Assert.False(form.IsOpen);
        Assert.Equal("Verbrauch 4,29 L/100 km", viewModel.Entries[0].ConsumptionText);
        Assert.Equal("Strecke 700 km", viewModel.Entries[0].DistanceText);
    }
}
