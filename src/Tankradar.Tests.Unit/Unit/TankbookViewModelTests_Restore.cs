using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Backup;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass das <see cref="TankbookViewModel"/> die Fahrzeugliste nach einer Wiederherstellung (H5) neu lädt.
/// </summary>
public class TankbookViewModelTests_Restore : BaseTest
{
    private readonly VehicleFixture _fixture = new();

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
    /// Prüft, dass nach der Benachrichtigung ein inzwischen vorhandenes Fahrzeug in der Liste erscheint.
    /// </summary>
    [Fact]
    public async Task Restored_ReloadsVehicles()
    {
        var notifier = new RestoreNotifier();
        var viewModel = new TankbookViewModel(_fixture.Service, new RecordingLogger<TankbookViewModel>(), new FakeScreenReaderAnnouncer(), notifier);
        viewModel.OnAppearing();
        await viewModel.LastTask;
        Assert.Empty(viewModel.Vehicles);

        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 10_000, null));
        notifier.NotifyRestored();
        await viewModel.LastTask;

        Assert.Equal(["Golf"], viewModel.Vehicles.Select(item => item.Name));
    }
}
