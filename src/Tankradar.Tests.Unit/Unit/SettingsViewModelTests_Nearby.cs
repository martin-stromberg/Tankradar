using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Schalter „In der Nähe“ im <see cref="SettingsViewModel"/>: sofortiges Speichern, stilles Laden, Hinweis bei Standortnutzung „Nie“ und Speicherfehler.
/// </summary>
public class SettingsViewModelTests_Nearby : BaseTest
{
    private readonly FakeSettingsService _service = new();

    /// <summary>
    /// Prüft, dass das Umschalten sofort speichert und der Wert in den gespeicherten Einstellungen steht.
    /// </summary>
    /// <param name="value">Der neue Wert des Schalters.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShowNearby_Toggle_SavesImmediately(bool value)
    {
        _service.Stored = AppSettings.CreateDefault() with { ShowNearby = !value };
        var viewModel = await CreateLoadedAsync();

        viewModel.ShowNearby = value;
        await viewModel.LastSaveTask;

        Assert.Equal(value, _service.Saved.Single().ShowNearby);
        Assert.False(viewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass das Laden den gespeicherten Wert übernimmt, ohne zu speichern.
    /// </summary>
    [Fact]
    public async Task ShowNearby_Load_AppliesWithoutSaving()
    {
        _service.Stored = AppSettings.CreateDefault() with { ShowNearby = true };

        var viewModel = await CreateLoadedAsync();

        Assert.True(viewModel.ShowNearby);
        Assert.Empty(_service.Saved);
    }

    /// <summary>
    /// Prüft, dass andere Einstellungen den Wert des Schalters beim Speichern mitnehmen.
    /// </summary>
    [Fact]
    public async Task ShowNearby_OtherChange_KeepsValueInSavedSettings()
    {
        _service.Stored = AppSettings.CreateDefault() with { ShowNearby = true };
        var viewModel = await CreateLoadedAsync();

        viewModel.ViewOptions.Single(o => o.Value == ResultView.Map).IsSelected = true;
        await viewModel.LastSaveTask;

        Assert.True(_service.Saved.Last().ShowNearby);
    }

    /// <summary>
    /// Prüft, dass der Hinweis nur bei der Standortnutzung „Nie“ erscheint und sich mit der Auswahl ändert.
    /// </summary>
    [Fact]
    public async Task GpsNever_ShowsNearbyHint()
    {
        var viewModel = await CreateLoadedAsync();
        Assert.False(viewModel.ShowNearbyGpsHint);

        viewModel.GpsOptions.Single(o => o.Value == GpsUsage.Never).IsSelected = true;
        await viewModel.LastSaveTask;
        Assert.True(viewModel.ShowNearbyGpsHint);
        Assert.Equal(SettingsTexts.NearbyGpsHint, viewModel.NearbyGpsHint);

        viewModel.GpsOptions.Single(o => o.Value == GpsUsage.Always).IsSelected = true;
        await viewModel.LastSaveTask;
        Assert.False(viewModel.ShowNearbyGpsHint);
    }

    /// <summary>
    /// Prüft, dass der Hinweis auch nach dem Laden einer gespeicherten Standortnutzung „Nie“ erscheint.
    /// </summary>
    [Fact]
    public async Task GpsNever_Load_ShowsNearbyHint()
    {
        _service.Stored = AppSettings.CreateDefault() with { GpsUsage = GpsUsage.Never };

        var viewModel = await CreateLoadedAsync();

        Assert.True(viewModel.ShowNearbyGpsHint);
    }

    /// <summary>
    /// Prüft, dass ein Speicherfehler beim Umschalten eine Meldung anzeigt.
    /// </summary>
    [Fact]
    public async Task Save_Fails_ShowsStatusMessage_ForNearby()
    {
        var viewModel = await CreateLoadedAsync();
        _service.SaveException = new InvalidOperationException("kaputt");

        viewModel.ShowNearby = true;
        await viewModel.LastSaveTask;

        Assert.Equal(SettingsTexts.SaveFailed, viewModel.StatusMessage);
    }

    private async Task<SettingsViewModel> CreateLoadedAsync()
    {
        var viewModel = new SettingsViewModel(_service, NullLogger<SettingsViewModel>.Instance);
        await viewModel.LoadAsync();
        return viewModel;
    }
}
