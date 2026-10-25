using Tankradar.MAUI.Services.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Meldungen des <see cref="MAUI.ViewModels.TankbookEntryFormViewModel"/>, wenn Kamera oder Fotoauswahl nicht genutzt werden können.
/// </summary>
public class TankbookEntryFormViewModelTests_PhotoAccess : TankbookEntriesViewModelTestBase
{
    /// <summary>
    /// Prüft, dass eine verweigerte Kamera-Freigabe eine eigene Meldung mit Hinweis auf die Einstellungen liefert, angesagt wird und nur der Ausnahmetyp ins Protokoll gelangt.
    /// </summary>
    [Fact]
    public async Task Capture_PermissionDenied_ShowsSettingsHint()
    {
        var viewModel = await OpenFormAsync();
        Picker.IsCaptureSupported = true;
        Picker.NextException = new ReceiptPhotoAccessException(ReceiptPhotoAccessProblem.PermissionDenied, new InvalidOperationException("geheimer Pfad /private/x"));

        await PressFormAsync(viewModel, viewModel.Form.CapturePhotoCommand);

        const string expected = "Kein Zugriff auf die Kamera. Bitte erlaube den Kamerazugriff in den Einstellungen unter „Tankatlas“.";
        Assert.Equal(expected, viewModel.Form.PhotoError);
        Assert.True(viewModel.Form.HasPhotoError);
        Assert.Equal([expected], Announcer.Announcements);
        Assert.False(viewModel.Form.HasPhoto);
        Assert.Contains(nameof(ReceiptPhotoAccessException), FormLogger.AllText);
        Assert.DoesNotContain("geheimer Pfad", FormLogger.AllText);
        Assert.True(viewModel.Form.CapturePhotoCommand.CanExecute(null));
    }

    /// <summary>
    /// Prüft, dass eine verweigerte Fotozugriffs-Freigabe bei der Auswahl eine eigene Meldung liefert.
    /// </summary>
    [Fact]
    public async Task Pick_PermissionDenied_ShowsSettingsHint()
    {
        var viewModel = await OpenFormAsync();
        Picker.NextException = new ReceiptPhotoAccessException(ReceiptPhotoAccessProblem.PermissionDenied);

        await PressFormAsync(viewModel, viewModel.Form.PickPhotoCommand);

        Assert.Equal("Kein Zugriff auf die Fotos. Bitte erlaube den Fotozugriff in den Einstellungen unter „Tankatlas“.", viewModel.Form.PhotoError);
        Assert.Equal([viewModel.Form.PhotoError], Announcer.Announcements);
    }

    /// <summary>
    /// Prüft, dass eine fehlende Geräteunterstützung eine verständliche Meldung statt des allgemeinen Fehlers liefert.
    /// </summary>
    [Fact]
    public async Task Capture_NotSupported_ShowsHint()
    {
        var viewModel = await OpenFormAsync();
        Picker.IsCaptureSupported = true;
        Picker.NextException = new ReceiptPhotoAccessException(ReceiptPhotoAccessProblem.NotSupported);

        await PressFormAsync(viewModel, viewModel.Form.CapturePhotoCommand);

        Assert.Equal("Auf diesem Gerät ist keine Fotoaufnahme möglich. Du kannst stattdessen ein Foto auswählen.", viewModel.Form.PhotoError);
    }

    /// <summary>
    /// Prüft, dass eine fehlende Geräteunterstützung bei der Auswahl eine eigene Meldung liefert.
    /// </summary>
    [Fact]
    public async Task Pick_NotSupported_ShowsHint()
    {
        var viewModel = await OpenFormAsync();
        Picker.NextException = new ReceiptPhotoAccessException(ReceiptPhotoAccessProblem.NotSupported);

        await PressFormAsync(viewModel, viewModel.Form.PickPhotoCommand);

        Assert.Equal("Auf diesem Gerät ist keine Fotoauswahl möglich.", viewModel.Form.PhotoError);
    }

    /// <summary>
    /// Prüft, dass sonstige Fehler weiterhin die allgemeine Meldung liefern und die Meldung beim nächsten Versuch zurückgesetzt wird.
    /// </summary>
    [Fact]
    public async Task Capture_OtherError_ShowsGenericMessageAndNextAttemptClears()
    {
        var viewModel = await OpenFormAsync();
        Picker.IsCaptureSupported = true;
        Picker.NextException = new IOException("kaputt");

        await PressFormAsync(viewModel, viewModel.Form.CapturePhotoCommand);

        Assert.Equal("Das Foto konnte nicht übernommen werden.", viewModel.Form.PhotoError);

        Picker.NextException = null;
        await PressFormAsync(viewModel, viewModel.Form.CapturePhotoCommand);

        Assert.False(viewModel.Form.HasPhotoError);
        Assert.True(viewModel.Form.HasPhoto);
    }

    /// <summary>
    /// Prüft, dass ein Abbruch durch den Nutzer weder Meldung noch Ansage erzeugt und die Befehle wieder ausführbar sind.
    /// </summary>
    /// <param name="capture"><see langword="true"/> für die Aufnahme, <see langword="false"/> für die Auswahl.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UserCancel_ShowsNoMessage(bool capture)
    {
        var viewModel = await OpenFormAsync();
        Picker.IsCaptureSupported = true;
        Picker.NextExtension = null;

        await PressFormAsync(viewModel, capture ? viewModel.Form.CapturePhotoCommand : viewModel.Form.PickPhotoCommand);

        Assert.False(viewModel.Form.HasPhotoError);
        Assert.Empty(Announcer.Announcements);
        Assert.False(viewModel.Form.HasPhoto);
        Assert.True(viewModel.Form.PickPhotoCommand.CanExecute(null));
        Assert.True(viewModel.Form.CapturePhotoCommand.CanExecute(null));
    }
}
