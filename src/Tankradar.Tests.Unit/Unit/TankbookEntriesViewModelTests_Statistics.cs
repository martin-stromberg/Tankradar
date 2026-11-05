using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die Auswertung auf der Seite <see cref="MAUI.ViewModels.TankbookEntriesViewModel"/> nach Erfassen, Korrigieren und Löschen sowie beim Fahrzeugwechsel aktuell ist.
/// </summary>
public class TankbookEntriesViewModelTests_Statistics : TankbookEntriesViewModelTestBase
{
    /// <summary>
    /// Prüft, dass das Erfassen über das Formular die Auswertung aktualisiert.
    /// </summary>
    [Fact]
    public async Task Entries_Save_RefreshesStatistics()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        Assert.False(viewModel.Statistics.HasKinds);

        await CreateViaFormAsync(viewModel, "40", "1,5", "60", "10500");
        await CreateViaFormAsync(viewModel, "30", "1,5", "45", "11000");

        var kind = Assert.Single(viewModel.Statistics.Kinds);
        Assert.Equal("6,00 L/100 km", $"{kind.AverageValueText} {kind.AverageUnitText}");
        Assert.Equal("9,00 €", $"{kind.CostPer100KmValueText} {kind.CostPer100KmUnitText}");
        Assert.Equal("105,00 €", kind.MonthCostText);
    }

    /// <summary>
    /// Prüft, dass das Korrigieren die Auswertung aktualisiert.
    /// </summary>
    [Fact]
    public async Task Entries_Edit_RefreshesStatistics()
    {
        var vehicle = await CreateVehicleAsync();
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-2), 10_500, 40m));
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-1), 11_000, 30m));
        var viewModel = await OpenAsync(vehicle.Id);
        Assert.Equal("6,00 L/100 km", $"{viewModel.Statistics.Kinds.Single().AverageValueText} {viewModel.Statistics.Kinds.Single().AverageUnitText}");

        viewModel.Entries[0].EditCommand.Execute(null);
        viewModel.Form.Quantity = "40";
        viewModel.Form.UnitPrice = "1,859";
        viewModel.Form.TotalAmount = "74,36";
        await PressFormAsync(viewModel, viewModel.Form.SaveCommand);

        Assert.Equal("8,00 L/100 km", $"{viewModel.Statistics.Kinds.Single().AverageValueText} {viewModel.Statistics.Kinds.Single().AverageUnitText}");
    }

    /// <summary>
    /// Prüft, dass das Löschen die Auswertung aktualisiert: Ohne zweiten vollen Vorgang gilt der Wert ab Startstand.
    /// </summary>
    [Fact]
    public async Task Entries_Delete_RefreshesStatistics()
    {
        var vehicle = await CreateVehicleAsync();
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-2), 10_500, 40m));
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-1), 11_000, 30m));
        var viewModel = await OpenAsync(vehicle.Id);
        var item = viewModel.Entries[0];

        await PressAsync(viewModel, item.DeleteCommand);
        await PressAsync(viewModel, item.ConfirmDeleteCommand);

        var kind = Assert.Single(viewModel.Statistics.Kinds);
        Assert.Single(viewModel.Entries);
        Assert.Equal(MAUI.Resources.Texts.TankbookStatisticsTexts.FromStartSource, kind.AverageSourceText);
        Assert.Equal("8,00", kind.AverageValueText);
        Assert.False(kind.HasNotice);
    }

    /// <summary>
    /// Prüft, dass der Wechsel des Fahrzeugs die Auswertung des vorigen Fahrzeugs verwirft und die des gewählten (ohne Vorgänge) zeigt.
    /// </summary>
    [Fact]
    public async Task SelectVehicle_ResetsStatistics()
    {
        var vehicle = await CreateVehicleAsync();
        var other = await Fixture.CreateVehicleAsync("Polo");
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-1), 10_500, 40m));
        var viewModel = await OpenAsync(vehicle.Id);
        Assert.True(viewModel.Statistics.HasKinds);

        await viewModel.SelectVehicleAsync(other.Id);

        Assert.False(viewModel.Statistics.HasKinds);
        Assert.Equal(MAUI.Resources.Texts.TankbookStatisticsTexts.NoEntriesNotice, viewModel.Statistics.Notice);
    }
}
