using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Laden der Seite <see cref="MAUI.ViewModels.TankbookEntriesViewModel"/>: Liste neueste zuerst mit Texten, Leerhinweis und Speicherfehler
/// (Auswahl des Fahrzeugs und Rückfall: <see cref="TankbookEntriesViewModelTests_Selection"/>).
/// </summary>
public class TankbookEntriesViewModelTests_Load : TankbookEntriesViewModelTestBase
{
    /// <summary>
    /// Prüft die Einträge neueste zuerst mit Beträgen, Strecke und Verbrauchstexten.
    /// </summary>
    [Fact]
    public async Task Load_ShowsEntriesNewestFirstWithConsumptionTexts()
    {
        var vehicle = await CreateVehicleAsync();
        await Fixture.CreateEntryAsync(vehicle.Id, new TankbookEntryInput(new DateTime(2026, 5, 17, 9, 0, 0), TankbookEntryKind.Fuel, 40m, 1.859m, 74.36m, 10_500, false, "Alpha Tankstelle", null, null, null));
        await Fixture.CreateEntryAsync(vehicle.Id, new TankbookEntryInput(new DateTime(2026, 5, 18, 14, 30, 0), TankbookEntryKind.Fuel, 30m, 1.859m, 55.77m, 11_000, false, "Beta Tankstelle", null, "Urlaub", null));

        var viewModel = await OpenAsync(vehicle.Id);

        Assert.Equal("Golf", viewModel.VehicleName);
        Assert.Equal("Diesel • Start 10.000 km", viewModel.VehicleDetails);
        Assert.Equal(["Beta Tankstelle", "Alpha Tankstelle"], viewModel.Entries.Select(item => item.StationName));
        var newest = viewModel.Entries[0];
        Assert.Equal("18.05.2026, 14:30", newest.DateText);
        Assert.Equal("55,77 €", newest.TotalText);
        Assert.Equal("1,859 €/L", newest.UnitPriceText);
        Assert.Equal("Menge 30,00 L", newest.QuantityText);
        Assert.Equal("Strecke 500 km", newest.DistanceText);
        Assert.Equal("Verbrauch 6,00 L/100 km", newest.ConsumptionText);
        Assert.Equal("„Urlaub“", newest.NoteText);
        Assert.False(newest.HasKindText);
        Assert.Equal("Verbrauch 8,00 L/100 km (ab Startstand)", viewModel.Entries[1].ConsumptionText);
        Assert.Equal("Strecke 500 km", viewModel.Entries[1].DistanceText);
        Assert.True(viewModel.Entries[1].HasConsumptionNote);
        Assert.False(newest.HasConsumptionNote);
        Assert.False(viewModel.ShowEmptyHint);
    }

    /// <summary>
    /// Prüft den Hinweis ohne Vorgänge.
    /// </summary>
    [Fact]
    public async Task Load_NoEntries_ShowsEmptyHint()
    {
        var vehicle = await CreateVehicleAsync();

        var viewModel = await OpenAsync(vehicle.Id);

        Assert.Empty(viewModel.Entries);
        Assert.True(viewModel.ShowEmptyHint);
        Assert.True(viewModel.Form.IsClosed);
    }

    /// <summary>
    /// Prüft, dass ein Speicherfehler beim Laden gemeldet wird und das Protokoll weder Namen noch Werte enthält.
    /// </summary>
    [Fact]
    public async Task Load_StorageFailure_ShowsMessageWithoutLoggingValues()
    {
        var vehicle = await Fixture.CreateVehicleAsync("Geheimauto");
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now, 10_500, stationName: "Geheimtankstelle"));
        Fixture.Factory.FailingCreations = 1;

        var viewModel = await OpenAsync(vehicle.Id);

        Assert.Equal("Die Tank- und Ladevorgänge konnten nicht geladen werden.", viewModel.StatusMessage);
        Assert.Equal([viewModel.StatusMessage!], Announcer.Announcements);
        Assert.False(viewModel.IsBusy);
        Assert.DoesNotContain("Geheim", Logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("10500", Logger.AllText, StringComparison.Ordinal);
    }
}
