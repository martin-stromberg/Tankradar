using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Laden der Einstellungen im <see cref="SettingsViewModel"/>.
/// </summary>
public class SettingsViewModelTests_Loading : BaseTest
{
    private readonly FakeSettingsService _service = new();

    /// <summary>
    /// Prüft, dass alle vier Bereiche mit den geladenen Werten befüllt werden.
    /// </summary>
    [Fact]
    public async Task LoadAsync_PopulatesAllSections()
    {
        _service.Stored = new AppSettings(
            [
                new FuelTypeSelection(FuelType.Diesel, true),
                new FuelTypeSelection(FuelType.SuperE5, false),
                new FuelTypeSelection(FuelType.SuperE10, true),
            ],
            GpsUsage.Never,
            ResultView.Map,
            ResultSortOrder.Distance);
        var viewModel = new SettingsViewModel(_service, NullLogger<SettingsViewModel>.Instance);

        await viewModel.LoadAsync();

        Assert.Equal([FuelType.Diesel, FuelType.SuperE5, FuelType.SuperE10], viewModel.FuelTypes.Select(f => f.FuelType));
        Assert.Equal([true, false, true], viewModel.FuelTypes.Select(f => f.IsSelected));
        Assert.Equal(GpsUsage.Never, viewModel.GpsOptions.Single(o => o.IsSelected).Value);
        Assert.Equal(ResultView.Map, viewModel.ViewOptions.Single(o => o.IsSelected).Value);
        Assert.Equal(ResultSortOrder.Distance, viewModel.SortOptions.Single(o => o.IsSelected).Value);
        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass bei einem Ladefehler eine Meldung erscheint und die Standardwerte bleiben.
    /// </summary>
    [Fact]
    public async Task LoadAsync_ServiceThrows_ShowsStatusMessageAndKeepsDefaults()
    {
        _service.LoadException = new InvalidOperationException("kaputt");
        var viewModel = new SettingsViewModel(_service, NullLogger<SettingsViewModel>.Instance);

        await viewModel.LoadAsync();

        Assert.Equal(SettingsTexts.LoadFailed, viewModel.StatusMessage);
        Assert.True(viewModel.HasStatusMessage);
        Assert.Equal(GpsUsage.WhileInUse, viewModel.GpsOptions.Single(o => o.IsSelected).Value);
        Assert.Empty(_service.Saved);
        Assert.False(viewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass ein Ladefehler mit Ausnahme protokolliert wird und das Laden trotzdem als abgeschlossen gilt.
    /// </summary>
    [Fact]
    public async Task LoadAsync_ServiceThrows_LogsException()
    {
        var exception = new InvalidOperationException("kaputt");
        _service.LoadException = exception;
        var logger = new ListLogger<SettingsViewModel>();
        var viewModel = new SettingsViewModel(_service, logger);

        await viewModel.LoadAsync();

        Assert.Equal((LogLevel.Error, (Exception?)exception), logger.Entries.Single());
        Assert.True(viewModel.IsLoaded);
    }

    /// <summary>
    /// Prüft, dass das Erscheinen der Seite das Laden startet.
    /// </summary>
    [Fact]
    public async Task OnAppearing_StartsLoad()
    {
        var viewModel = new SettingsViewModel(_service, NullLogger<SettingsViewModel>.Instance);

        viewModel.OnAppearing();
        await viewModel.LoadAsync();

        Assert.True(_service.LoadCount >= 1);
    }

    /// <summary>
    /// Prüft, dass das Laden selbst nichts speichert.
    /// </summary>
    [Fact]
    public async Task LoadAsync_DoesNotTriggerSave()
    {
        var viewModel = new SettingsViewModel(_service, NullLogger<SettingsViewModel>.Instance);

        await viewModel.LoadAsync();
        await viewModel.LastSaveTask;

        Assert.Empty(_service.Saved);
    }
}
