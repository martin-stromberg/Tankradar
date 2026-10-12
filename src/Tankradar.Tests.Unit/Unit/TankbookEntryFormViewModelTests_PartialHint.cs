using System.ComponentModel;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Hinweis zur Teilbetankung im Formular <see cref="MAUI.ViewModels.TankbookEntryFormViewModel"/>.
/// </summary>
public class TankbookEntryFormViewModelTests_PartialHint : TankbookEntriesViewModelTestBase
{
    /// <summary>
    /// Prüft, dass der Hinweis sichtbar ist, solange „Teilbetankung“ aktiv ist, und die Änderung gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Partial_On_ShowsHint()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        viewModel.AddEntryCommand.Execute(null);
        var changed = new List<string?>();
        viewModel.Form.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.Form.IsPartial = true;

        Assert.True(viewModel.Form.ShowPartialHint);
        Assert.Equal(TankbookEntryTexts.PartialHint, viewModel.Form.PartialHintText);
        Assert.Contains(nameof(viewModel.Form.ShowPartialHint), changed);
    }

    /// <summary>
    /// Prüft, dass der Hinweis bei ausgeschaltetem Schalter verborgen ist.
    /// </summary>
    [Fact]
    public async Task Partial_Off_HidesHint()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        viewModel.AddEntryCommand.Execute(null);
        Assert.False(viewModel.Form.ShowPartialHint);

        viewModel.Form.IsPartial = true;
        viewModel.Form.IsPartial = false;

        Assert.False(viewModel.Form.ShowPartialHint);
    }
}
