using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Einstieg in die Tank- und Ladevorgänge aus dem <see cref="TankbookViewModel"/> über die Fahrzeugkarte.
/// </summary>
public class TankbookViewModelTests_Navigation : BaseTest
{
    private readonly VehicleFixture _fixture = new();
    private readonly FakeTankbookNavigator _navigator = new();

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
    /// Prüft, dass „Tank- und Ladevorgänge“ die Vorgänge des gewählten Fahrzeugs öffnet (Auswahl per Karte, nicht per Kennung)
    /// und der Hinweis der Schaltfläche das Fahrzeug nennt.
    /// </summary>
    [Fact]
    public async Task OpenEntries_NavigatesWithVehicleId()
    {
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 1000, null));
        var polo = (await _fixture.Service.CreateVehicleAsync(new VehicleInput("Polo", VehicleFuelKind.Petrol, 2000, null))).Vehicle!;
        var viewModel = new TankbookViewModel(_fixture.Service, _navigator, new RecordingLogger<TankbookViewModel>(), new FakeScreenReaderAnnouncer());
        viewModel.OnAppearing();
        await viewModel.LastTask;
        var item = viewModel.Vehicles.Single(vehicle => vehicle.Name == "Polo");

        item.OpenEntriesCommand.Execute(null);
        await viewModel.LastTask;

        Assert.Equal([polo.Id], _navigator.Opened);
        Assert.Equal("Fahrzeug „Polo“", item.VehicleHint);
    }
}
