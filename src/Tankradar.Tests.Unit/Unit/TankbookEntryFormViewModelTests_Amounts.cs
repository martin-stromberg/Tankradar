using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Rechenhinweis und Plausibilität von Menge, Preis und Gesamtbetrag im <see cref="MAUI.ViewModels.TankbookEntryFormViewModel"/>.
/// </summary>
public class TankbookEntryFormViewModelTests_Amounts : TankbookEntriesViewModelTestBase
{
    /// <summary>
    /// Prüft den Rechenhinweis für jeden aus zwei Werten berechneten dritten Wert und dass er bei drei oder einem Wert verschwindet.
    /// </summary>
    [Fact]
    public async Task TwoAmounts_ShowsComputedHint()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        viewModel.AddEntryCommand.Execute(null);
        var form = viewModel.Form;

        form.Quantity = "40";
        Assert.False(form.HasComputedHint);
        form.UnitPrice = "1,859";
        Assert.Equal("Ergibt 74,36 €", form.ComputedHint);
        form.UnitPrice = string.Empty;
        form.TotalAmount = "55,77";
        Assert.Equal("Ergibt 1,394 €/L", form.ComputedHint);
        form.Quantity = string.Empty;
        form.UnitPrice = "1,859";
        Assert.Equal("Ergibt 30,00 L", form.ComputedHint);
        form.Quantity = "30";
        Assert.False(form.HasComputedHint);
        form.Quantity = "x";
        Assert.Equal(string.Empty, form.ComputedHint);
    }

    /// <summary>
    /// Prüft, dass drei abweichende Werte als Fehler am Gesamtbetrag mit rechnerischem Betrag gemeldet werden und ein einzelner Wert als fehlend.
    /// </summary>
    [Fact]
    public async Task Save_AmountMismatchOrMissing_ShowsTotalAmountError()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);

        await CreateViaFormAsync(viewModel, "40", "1,859", "75,00", "10500");

        Assert.Equal("Menge × Preis ergibt 74,36 €, nicht 75,00 €. Bitte prüfe die Werte.", viewModel.Form.TotalAmountError);

        viewModel.Form.UnitPrice = string.Empty;
        viewModel.Form.TotalAmount = string.Empty;
        await PressFormAsync(viewModel, viewModel.Form.SaveCommand);

        Assert.Equal("Bitte gib mindestens zwei der drei Werte Menge, Preis pro Liter und Gesamtbetrag an.", viewModel.Form.TotalAmountError);
        Assert.Empty(await Fixture.Service.GetEntriesAsync(vehicle.Id));
    }

    /// <summary>
    /// Prüft, dass bei einem Elektrofahrzeug die Einheiten kWh gelten und die Meldung „Preis pro kWh“ nennt.
    /// </summary>
    [Fact]
    public async Task Electric_UsesKwhUnits()
    {
        var vehicle = await CreateVehicleAsync(VehicleFuelKind.Electric);
        var viewModel = await OpenAsync(vehicle.Id);

        await CreateViaFormAsync(viewModel, "40", string.Empty, string.Empty, "10500", "Zuhause");

        Assert.Equal("Strom", viewModel.Form.KindText);
        Assert.Equal("kWh", viewModel.Form.QuantityUnit);
        Assert.Equal("Bitte gib mindestens zwei der drei Werte Menge, Preis pro kWh und Gesamtbetrag an.", viewModel.Form.TotalAmountError);
        Assert.Equal(TankbookEntryKind.Electricity, viewModel.Form.SelectedKind);
    }
}
