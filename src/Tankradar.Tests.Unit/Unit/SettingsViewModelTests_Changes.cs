using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das sofortige Speichern und die Regeln beim Ändern von Einstellungen im <see cref="SettingsViewModel"/>.
/// </summary>
public class SettingsViewModelTests_Changes : BaseTest
{
    private readonly FakeSettingsService _service = new();

    /// <summary>
    /// Prüft, dass jede Standortnutzung beim Auswählen sofort gespeichert wird.
    /// </summary>
    /// <param name="usage">Die ausgewählte Standortnutzung.</param>
    [Theory]
    [InlineData(GpsUsage.Always)]
    [InlineData(GpsUsage.WhileInUse)]
    [InlineData(GpsUsage.Never)]
    public async Task SelectGps_SavesImmediately(GpsUsage usage)
    {
        var viewModel = await CreateLoadedAsync();
        viewModel.GpsOptions.First(o => o.Value != usage).IsSelected = true;
        await viewModel.LastSaveTask;

        viewModel.GpsOptions.Single(o => o.Value == usage).IsSelected = true;
        await viewModel.LastSaveTask;

        Assert.Equal(usage, _service.Saved.Last().GpsUsage);
        Assert.Equal(usage, viewModel.GpsOptions.Single(o => o.IsSelected).Value);
    }

    /// <summary>
    /// Prüft, dass jede Ergebnisansicht beim Auswählen sofort gespeichert wird.
    /// </summary>
    /// <param name="view">Die ausgewählte Ansicht.</param>
    [Theory]
    [InlineData(ResultView.List)]
    [InlineData(ResultView.Map)]
    public async Task SelectView_SavesImmediately(ResultView view)
    {
        var viewModel = await CreateLoadedAsync();
        viewModel.ViewOptions.First(o => o.Value != view).IsSelected = true;
        await viewModel.LastSaveTask;

        viewModel.ViewOptions.Single(o => o.Value == view).IsSelected = true;
        await viewModel.LastSaveTask;

        Assert.Equal(view, _service.Saved.Last().ResultView);
    }

    /// <summary>
    /// Prüft, dass jede Ergebnissortierung beim Auswählen sofort gespeichert wird.
    /// </summary>
    /// <param name="sortOrder">Die ausgewählte Sortierung.</param>
    [Theory]
    [InlineData(ResultSortOrder.Price)]
    [InlineData(ResultSortOrder.Distance)]
    [InlineData(ResultSortOrder.Name)]
    public async Task SelectSort_SavesImmediately(ResultSortOrder sortOrder)
    {
        var viewModel = await CreateLoadedAsync();
        viewModel.SortOptions.First(o => o.Value != sortOrder).IsSelected = true;
        await viewModel.LastSaveTask;

        viewModel.SortOptions.Single(o => o.Value == sortOrder).IsSelected = true;
        await viewModel.LastSaveTask;

        Assert.Equal(sortOrder, _service.Saved.Last().ResultSortOrder);
    }

    /// <summary>
    /// Prüft, dass eine neue Auswahl die übrigen Optionen der Gruppe abwählt.
    /// </summary>
    [Fact]
    public async Task SelectGps_DeselectsOtherOptionsOfGroup()
    {
        var viewModel = await CreateLoadedAsync();

        viewModel.GpsOptions.Single(o => o.Value == GpsUsage.Never).IsSelected = true;
        await viewModel.LastSaveTask;

        Assert.Single(viewModel.GpsOptions, o => o.IsSelected);
    }

    /// <summary>
    /// Prüft, dass Abwählen einer Spritsorte sofort gespeichert wird.
    /// </summary>
    [Fact]
    public async Task ToggleFuel_SavesImmediately()
    {
        var viewModel = await CreateLoadedAsync();

        viewModel.FuelTypes.Single(f => f.FuelType == FuelType.SuperE10).IsSelected = false;
        await viewModel.LastSaveTask;

        var saved = _service.Saved.Single();
        Assert.False(saved.FuelTypes.Single(f => f.FuelType == FuelType.SuperE10).IsSelected);
        Assert.False(viewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass Verschieben nach oben die Reihenfolge ändert und speichert.
    /// </summary>
    [Fact]
    public async Task MoveUp_ChangesOrderAndSaves()
    {
        var viewModel = await CreateLoadedAsync();

        viewModel.FuelTypes.Single(f => f.FuelType == FuelType.Diesel).MoveUpCommand.Execute(null);
        await viewModel.LastSaveTask;

        Assert.Equal([FuelType.SuperE5, FuelType.Diesel, FuelType.SuperE10], viewModel.FuelTypes.Select(f => f.FuelType));
        Assert.Equal([FuelType.SuperE5, FuelType.Diesel, FuelType.SuperE10], _service.Saved.Single().FuelTypes.Select(f => f.FuelType));
    }

    /// <summary>
    /// Prüft, dass Verschieben nach unten die Reihenfolge ändert und speichert.
    /// </summary>
    [Fact]
    public async Task MoveDown_ChangesOrderAndSaves()
    {
        var viewModel = await CreateLoadedAsync();

        viewModel.FuelTypes.Single(f => f.FuelType == FuelType.SuperE5).MoveDownCommand.Execute(null);
        await viewModel.LastSaveTask;

        Assert.Equal([FuelType.SuperE10, FuelType.SuperE5, FuelType.Diesel], viewModel.FuelTypes.Select(f => f.FuelType));
        Assert.Equal([FuelType.SuperE10, FuelType.SuperE5, FuelType.Diesel], _service.Saved.Single().FuelTypes.Select(f => f.FuelType));
    }

    /// <summary>
    /// Prüft, dass die erste Zeile nicht nach oben und die letzte nicht nach unten verschiebbar ist, auch nach dem Verschieben.
    /// </summary>
    [Fact]
    public async Task CanMove_FirstAndLast()
    {
        var viewModel = await CreateLoadedAsync();

        Assert.False(viewModel.FuelTypes[0].CanMoveUp);
        Assert.True(viewModel.FuelTypes[0].CanMoveDown);
        Assert.True(viewModel.FuelTypes[2].CanMoveUp);
        Assert.False(viewModel.FuelTypes[2].CanMoveDown);

        viewModel.FuelTypes[2].MoveUpCommand.Execute(null);
        await viewModel.LastSaveTask;

        Assert.False(viewModel.FuelTypes[0].CanMoveUp);
        Assert.True(viewModel.FuelTypes[2].CanMoveUp);
        Assert.False(viewModel.FuelTypes[2].CanMoveDown);
    }

    /// <summary>
    /// Prüft, dass das Abwählen der letzten Spritsorte zurückgenommen, gemeldet und nicht gespeichert wird.
    /// </summary>
    [Fact]
    public async Task DeselectLastFuel_IsRevertedWithMessageAndNotSaved()
    {
        var viewModel = await CreateLoadedAsync();
        viewModel.FuelTypes[0].IsSelected = false;
        viewModel.FuelTypes[1].IsSelected = false;
        await viewModel.LastSaveTask;
        var savedBefore = _service.Saved.Count;

        viewModel.FuelTypes[2].IsSelected = false;
        await viewModel.LastSaveTask;

        Assert.True(viewModel.FuelTypes[2].IsSelected);
        Assert.Equal(SettingsTexts.LastFuelTypeRequired, viewModel.StatusMessage);
        Assert.Equal(savedBefore, _service.Saved.Count);
    }

    /// <summary>
    /// Prüft, dass ein Speicherfehler eine Meldung anzeigt und ein späterer Erfolg sie löscht.
    /// </summary>
    [Fact]
    public async Task Save_Fails_ShowsStatusMessage()
    {
        var viewModel = await CreateLoadedAsync();
        _service.SaveException = new InvalidOperationException("kaputt");

        viewModel.GpsOptions.Single(o => o.Value == GpsUsage.Never).IsSelected = true;
        await viewModel.LastSaveTask;

        Assert.Equal(SettingsTexts.SaveFailed, viewModel.StatusMessage);

        _service.SaveException = null;
        viewModel.ViewOptions.Single(o => o.Value == ResultView.Map).IsSelected = true;
        await viewModel.LastSaveTask;

        Assert.False(viewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein Speicherfehler mit Ausnahme protokolliert wird.
    /// </summary>
    [Fact]
    public async Task Save_Fails_LogsException()
    {
        var logger = new ListLogger<SettingsViewModel>();
        var viewModel = new SettingsViewModel(_service, logger);
        await viewModel.LoadAsync();
        var exception = new InvalidOperationException("kaputt");
        _service.SaveException = exception;

        viewModel.GpsOptions.Single(o => o.Value == GpsUsage.Never).IsSelected = true;
        await viewModel.LastSaveTask;

        Assert.Equal((LogLevel.Error, (Exception?)exception), logger.Entries.Single());
    }

    private async Task<SettingsViewModel> CreateLoadedAsync()
    {
        var viewModel = new SettingsViewModel(_service, NullLogger<SettingsViewModel>.Instance);
        await viewModel.LoadAsync();
        return viewModel;
    }
}
