using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Laden der Fahrzeugliste im <see cref="TankbookViewModel"/>: Einträge mit Angaben, Leerhinweis und Speicherfehler.
/// </summary>
public class TankbookViewModelTests_Load : BaseTest
{
    private readonly VehicleFixture _fixture = new();
    private readonly RecordingLogger<TankbookViewModel> _logger = new();
    private readonly FakeScreenReaderAnnouncer _announcer = new();

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
    /// Prüft, dass die Fahrzeuge nach Namen geordnet mit ihren Angaben erscheinen.
    /// </summary>
    [Fact]
    public async Task Load_ShowsVehiclesWithDetails()
    {
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Zoe", VehicleFuelKind.Electric, 12000, null));
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 84350, 5.4m));
        var viewModel = new TankbookViewModel(_fixture.Service, _logger, _announcer);

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.True(viewModel.IsLoaded);
        Assert.True(viewModel.HasVehicles);
        Assert.False(viewModel.ShowEmptyHint);
        Assert.False(viewModel.IsBusy);
        Assert.Equal(["Golf", "Zoe"], viewModel.Vehicles.Select(item => item.Name));
        Assert.Equal("Diesel • Start 84.350 km • Startverbrauch 5,4 L/100 km", viewModel.Vehicles[0].Details);
        Assert.Equal("Elektro • Start 12.000 km", viewModel.Vehicles[1].Details);
        Assert.Equal("Tankbuch", viewModel.Title);
    }

    /// <summary>
    /// Prüft, dass ohne Fahrzeuge der Leerhinweis erscheint (erst nach dem Laden).
    /// </summary>
    [Fact]
    public async Task Load_NoVehicles_ShowsEmptyHint()
    {
        var viewModel = new TankbookViewModel(_fixture.Service, _logger, _announcer);
        Assert.False(viewModel.ShowEmptyHint);

        await viewModel.LoadAsync();

        Assert.True(viewModel.ShowEmptyHint);
        Assert.False(viewModel.HasVehicles);
        Assert.False(viewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein Speicherfehler beim Laden gemeldet und ohne Fahrzeugnamen protokolliert wird.
    /// </summary>
    [Fact]
    public async Task Load_StorageFailure_ShowsMessageWithoutLoggingNames()
    {
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Geheimauto", VehicleFuelKind.Petrol, 1, null));
        var viewModel = new TankbookViewModel(_fixture.Service, _logger, _announcer);
        _fixture.Factory.FailingCreations = 1;

        await viewModel.LoadAsync();

        Assert.Equal("Die Fahrzeuge konnten nicht geladen werden.", viewModel.StatusMessage);
        Assert.True(viewModel.HasStatusMessage);
        Assert.False(viewModel.IsLoaded);
        Assert.Contains("InvalidOperationException", _logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("Geheimauto", _logger.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass die Meldung eines fehlgeschlagenen Ladens beim erneuten Erscheinen der Seite verschwindet, wenn das Laden dann gelingt.
    /// </summary>
    [Fact]
    public async Task Load_AfterFailure_AppearingAgain_HidesMessage()
    {
        var viewModel = new TankbookViewModel(_fixture.Service, _logger, _announcer);
        _fixture.Factory.FailingCreations = 1;
        viewModel.OnAppearing();
        await viewModel.LastTask;
        Assert.True(viewModel.HasStatusMessage);

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.False(viewModel.HasStatusMessage);
        Assert.True(viewModel.IsLoaded);
        Assert.True(viewModel.ShowEmptyHint);
    }
}
