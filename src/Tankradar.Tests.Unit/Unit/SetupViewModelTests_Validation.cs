using Tankradar.MAUI.Resources.Texts;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Regel „mindestens eine Spritsorte“ beim Speichern der Einrichtung.
/// </summary>
public class SetupViewModelTests_Validation : BaseTest
{
    private readonly FakeSettingsService _service = new();

    /// <summary>
    /// Prüft, dass ohne ausgewählte Sorte eine Meldung erscheint, nichts gespeichert und die Einrichtung nicht abgeschlossen wird.
    /// </summary>
    [Fact]
    public async Task SetupViewModel_NoFuelTypeSelected_ShowsMessageAndDoesNotSave()
    {
        var viewModel = SetupViewModelFactory.CreateSetupViewModel(_service);
        var completed = 0;
        viewModel.Completed += (_, _) => completed++;
        foreach (var item in viewModel.FuelTypes)
        {
            item.IsSelected = false;
        }

        await viewModel.SaveAsync();

        Assert.Equal(SetupTexts.FuelTypeRequired, viewModel.StatusMessage);
        Assert.True(viewModel.HasStatusMessage);
        Assert.Empty(_service.Saved);
        Assert.Equal(0, completed);
    }
}
