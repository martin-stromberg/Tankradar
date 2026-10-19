using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Auswahl der Tankstelle im <see cref="MAUI.ViewModels.TankbookEntryFormViewModel"/>: bekannte Tankstellen per Namen, Kennung nur intern.
/// </summary>
public class TankbookEntryFormViewModelTests_Station : TankbookEntriesViewModelTestBase
{
    /// <summary>
    /// Prüft, dass ein Chip Name und Kennung übernimmt, als ausgewählt markiert ist und die Kennung gespeichert wird.
    /// </summary>
    [Fact]
    public async Task SelectKnownStation_FillsNameAndKeepsStationId()
    {
        await Fixture.AddFavoriteStationAsync(TankbookEntryFixture.AlphaStationId, "Alpha Tankstelle", "Berlin");
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        viewModel.AddEntryCommand.Execute(null);
        var form = viewModel.Form;
        var chip = Assert.Single(form.KnownStations);
        Assert.Equal("Alpha Tankstelle", chip.Label);

        chip.SelectCommand.Execute(null);

        Assert.Equal("Alpha Tankstelle", form.StationName);
        Assert.Equal(TankbookEntryFixture.AlphaStationId, form.StationId);
        Assert.True(chip.IsSelected);
        Assert.Equal("Ausgewählt", chip.SelectionHint);

        form.Quantity = "40";
        form.UnitPrice = "1,859";
        form.Odometer = "10500";
        await PressFormAsync(viewModel, form.SaveCommand);

        Assert.Equal(TankbookEntryFixture.AlphaStationId, viewModel.Entries[0].Entry.StationId);
    }

    /// <summary>
    /// Prüft, dass das Ändern des Namens von Hand die Kennung entfernt und den Chip abwählt.
    /// </summary>
    [Fact]
    public async Task EditStationName_ClearsStationId()
    {
        await Fixture.AddFavoriteStationAsync(TankbookEntryFixture.AlphaStationId, "Alpha Tankstelle", "Berlin");
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        viewModel.AddEntryCommand.Execute(null);
        var form = viewModel.Form;
        form.KnownStations[0].SelectCommand.Execute(null);

        form.StationName = "Alpha Tankstelle Nord";

        Assert.Null(form.StationId);
        Assert.False(form.KnownStations[0].IsSelected);
    }

    /// <summary>
    /// Prüft, dass gleichnamige Tankstellen zur Unterscheidung den Ort tragen.
    /// </summary>
    [Fact]
    public async Task SameNames_LabelsIncludePlace()
    {
        await Fixture.AddFavoriteStationAsync(TankbookEntryFixture.AlphaStationId, "Alpha Tankstelle", "Berlin");
        await Fixture.AddFavoriteStationAsync("00000002-0000-4000-8000-000000000000", "Alpha Tankstelle", "Potsdam");
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);

        Assert.Equal(["Alpha Tankstelle (Berlin)", "Alpha Tankstelle (Potsdam)"], viewModel.Form.KnownStations.Select(chip => chip.Label));
    }

    /// <summary>
    /// Prüft, dass zwei Tankstellen gleicher Marke im selben Ort durch Stammdatenname bzw. Straße unterscheidbar beschriftet sind.
    /// </summary>
    [Fact]
    public async Task SameBrandSamePlace_LabelsIncludeMasterName()
    {
        await Fixture.AddFavoriteStationAsync(TankbookEntryFixture.AlphaStationId, "JET BERLIN HAUPTSTR. 1", "Berlin", brand: "JET");
        await Fixture.AddFavoriteStationAsync("00000002-0000-4000-8000-000000000000", "JET BERLIN NEBENWEG 22", "Berlin", brand: "JET");
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);

        Assert.Equal(["JET · JET BERLIN HAUPTSTR. 1", "JET · JET BERLIN NEBENWEG 22"], viewModel.Form.KnownStations.Select(chip => chip.Label));
    }
}
