using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Belegfoto im <see cref="MAUI.ViewModels.TankbookEntryFormViewModel"/>: Auswahl, Fehler, Entfernen, Abbrechen und Aufnahme.
/// </summary>
public class TankbookEntryFormViewModelTests_Photo : TankbookEntriesViewModelTestBase
{
    private async Task<MAUI.ViewModels.TankbookEntriesViewModel> OpenFormAsync()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        viewModel.AddEntryCommand.Execute(null);
        return viewModel;
    }

    /// <summary>
    /// Prüft, dass ein ausgewähltes Foto in die Ablage übernommen und mit dem Vorgang gespeichert wird.
    /// </summary>
    [Fact]
    public async Task PickPhoto_ImportsAndShowsPreview()
    {
        var viewModel = await OpenFormAsync();
        var form = viewModel.Form;

        await PressFormAsync(viewModel, form.PickPhotoCommand);

        Assert.True(form.HasPhoto);
        var fileName = form.PhotoFileName!;
        Assert.Contains(fileName, Fixture.Photos.Files);
        form.StationName = "Alpha Tankstelle";
        form.Quantity = "40";
        form.UnitPrice = "1,859";
        form.Odometer = "10500";
        await PressFormAsync(viewModel, form.SaveCommand);

        Assert.Equal(fileName, viewModel.Entries[0].Entry.ReceiptPhotoFileName);
        Assert.True(viewModel.Entries[0].HasReceipt);
        Assert.Contains(fileName, Fixture.Photos.Files);
        viewModel.Entries[0].ShowReceiptCommand.Execute(null);
        Assert.True(viewModel.Entries[0].IsReceiptImageVisible);
        Assert.Equal("Beleg ausblenden", viewModel.Entries[0].ReceiptButtonLabel);
    }

    /// <summary>
    /// Prüft, dass ein nicht unterstütztes Format als Fehler erscheint, angesagt wird und kein Foto übernommen wird.
    /// </summary>
    [Fact]
    public async Task PickPhoto_Unsupported_ShowsError()
    {
        var viewModel = await OpenFormAsync();
        Fixture.Photos.NextImportError = ReceiptPhotoError.UnsupportedFormat;

        await PressFormAsync(viewModel, viewModel.Form.PickPhotoCommand);

        Assert.Equal("Dieses Dateiformat wird nicht unterstützt. Bitte wähle ein JPG-, PNG- oder HEIC-Foto.", viewModel.Form.PhotoError);
        Assert.Equal([viewModel.Form.PhotoError], Announcer.Announcements);
        Assert.False(viewModel.Form.HasPhoto);
    }

    /// <summary>
    /// Prüft, dass „Foto entfernen“ ein neu ausgewähltes Foto aus der Ablage löscht und ein Ersetzen das vorige ausstehende Foto löscht.
    /// </summary>
    [Fact]
    public async Task RemovePhoto_ClearsPendingFile()
    {
        var viewModel = await OpenFormAsync();
        var form = viewModel.Form;
        await PressFormAsync(viewModel, form.PickPhotoCommand);
        var first = form.PhotoFileName!;
        await PressFormAsync(viewModel, form.PickPhotoCommand);
        var second = form.PhotoFileName!;

        form.RemovePhotoCommand.Execute(null);

        Assert.False(form.HasPhoto);
        Assert.Equal([first, second], Fixture.Photos.Deleted);
        Assert.Empty(Fixture.Photos.Files);
    }

    /// <summary>
    /// Prüft, dass „Abbrechen“ ein neu ausgewähltes Foto löscht, ein gespeichertes Foto beim Abbrechen einer Korrektur aber bleibt.
    /// </summary>
    [Fact]
    public async Task Cancel_DeletesPendingPhoto()
    {
        var vehicle = await CreateVehicleAsync();
        var stored = Fixture.Photos.AddFile();
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now, 10_500, photo: stored));
        var viewModel = await OpenAsync(vehicle.Id);
        viewModel.Entries[0].EditCommand.Execute(null);
        var form = viewModel.Form;
        Assert.Equal(stored, form.PhotoFileName);
        await PressFormAsync(viewModel, form.PickPhotoCommand);
        var pending = form.PhotoFileName!;

        form.CancelCommand.Execute(null);

        Assert.False(form.IsOpen);
        Assert.Equal([pending], Fixture.Photos.Deleted);
        Assert.Contains(stored, Fixture.Photos.Files);
    }

    /// <summary>
    /// Prüft, dass „Foto aufnehmen“ nur angeboten wird, wenn das Gerät Aufnahmen unterstützt, und dann die Kamera nutzt; ein Abbruch ändert nichts.
    /// </summary>
    [Fact]
    public async Task Capture_HiddenWhenNotSupported()
    {
        var viewModel = await OpenFormAsync();
        Assert.False(viewModel.Form.IsCaptureSupported);

        Picker.IsCaptureSupported = true;
        Assert.True(viewModel.Form.IsCaptureSupported);
        await PressFormAsync(viewModel, viewModel.Form.CapturePhotoCommand);
        Assert.Equal(1, Picker.CaptureCalls);
        Assert.True(viewModel.Form.HasPhoto);

        Picker.NextExtension = null;
        var before = viewModel.Form.PhotoFileName;
        await PressFormAsync(viewModel, viewModel.Form.PickPhotoCommand);
        Assert.Equal(before, viewModel.Form.PhotoFileName);
    }
}
