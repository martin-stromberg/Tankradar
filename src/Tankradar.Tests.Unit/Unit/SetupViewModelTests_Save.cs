using Tankradar.MAUI.Models;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Speichern der Einrichtung über <see cref="MAUI.Services.ISettingsService"/> und den Fehlerpfad.
/// </summary>
public class SetupViewModelTests_Save : BaseTest
{
    private readonly FakeSettingsService _service = new();

    /// <summary>
    /// Prüft, dass Speichern die Auswahl mit den übrigen Standardwerten schreibt und <c>Completed</c> auslöst.
    /// </summary>
    [Fact]
    public async Task SetupViewModel_Save_PersistsSelectionAndRaisesCompleted()
    {
        var viewModel = SetupViewModelFactory.CreateSetupViewModel(_service);
        var completed = 0;
        viewModel.Completed += (_, _) => completed++;
        viewModel.FuelTypes.Single(item => item.FuelType == FuelType.SuperE5).IsSelected = false;
        viewModel.FuelTypes.Single(item => item.FuelType == FuelType.SuperE10).IsSelected = false;

        await viewModel.SaveAsync();

        var expected = AppSettings.CreateDefault() with
        {
            FuelTypes =
            [
                new FuelTypeSelection(FuelType.SuperE5, false),
                new FuelTypeSelection(FuelType.SuperE10, false),
                new FuelTypeSelection(FuelType.Diesel, true),
            ],
        };
        var saved = Assert.Single(_service.Saved);
        Assert.Equal(expected.FuelTypes, saved.FuelTypes);
        Assert.Equal(expected.GpsUsage, saved.GpsUsage);
        Assert.Equal(expected.ResultView, saved.ResultView);
        Assert.Equal(expected.ResultSortOrder, saved.ResultSortOrder);
        Assert.Equal(expected.ShowNearby, saved.ShowNearby);
        Assert.Equal(1, completed);
        Assert.False(viewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein Speicherfehler als Statusmeldung erscheint, <c>Completed</c> ausbleibt und die Einrichtung bedienbar bleibt.
    /// </summary>
    [Fact]
    public async Task SetupViewModel_Save_Fails_ShowsStatusAndStaysOpen()
    {
        _service.SaveException = new InvalidOperationException("Datenbank gesperrt");
        var viewModel = SetupViewModelFactory.CreateSetupViewModel(_service);
        var completed = 0;
        viewModel.Completed += (_, _) => completed++;

        await viewModel.SaveAsync();

        Assert.Equal(SetupTexts.SaveFailed, viewModel.StatusMessage);
        Assert.Equal(0, completed);
        Assert.False(viewModel.IsBusy);
        Assert.Empty(_service.Saved);
    }

    /// <summary>
    /// Prüft, dass ein zweites Auslösen während eines laufenden Speichervorgangs (z. B. Doppelklick) nichts erneut speichert
    /// und <c>Completed</c> nur einmal ausgelöst wird.
    /// </summary>
    [Fact]
    public async Task SetupViewModel_Save_WhileBusy_SavesOnlyOnce()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _service.SaveGate = gate.Task;
        var viewModel = SetupViewModelFactory.CreateSetupViewModel(_service);
        var completed = 0;
        viewModel.Completed += (_, _) => completed++;

        var first = viewModel.SaveAsync();
        Assert.True(viewModel.IsBusy);
        var second = viewModel.SaveAsync();
        await second;
        gate.SetResult();
        await first;

        Assert.Equal(1, _service.SaveCount);
        Assert.Single(_service.Saved);
        Assert.Equal(1, completed);
        Assert.False(viewModel.IsBusy);
    }
}
