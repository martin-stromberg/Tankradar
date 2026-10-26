using Tankradar.MAUI.Services.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft <see cref="MauiReceiptPhotoPicker"/>: Optionen der Aufnahme und die Abbildung der Plattformausnahmen.
/// </summary>
public class MauiReceiptPhotoPickerTests : BaseTest
{
    private readonly FakeMediaPicker _media = new();

    private MauiReceiptPhotoPicker CreatePicker()
    {
        return new MauiReceiptPhotoPicker(_media);
    }

    /// <summary>
    /// Prüft, dass die Aufnahme mit JPEG-Qualität unter 90 angefordert wird (sonst liefert iOS ein PNG in voller Auflösung) und die Endung des Ergebnisses übernommen wird.
    /// </summary>
    [Fact]
    public async Task Capture_RequestsJpegQualityAndKeepsExtension()
    {
        _media.CaptureResult = new FileResult(Path.Combine(Path.GetTempPath(), "beleg.jpg"));

        var photo = await CreatePicker().CaptureAsync();

        Assert.NotNull(photo);
        Assert.Equal(".jpg", photo.Extension);
        Assert.Equal(MauiReceiptPhotoPicker.CaptureQuality, _media.CaptureOptions?.CompressionQuality);
        Assert.True(MauiReceiptPhotoPicker.CaptureQuality < 90);
    }

    /// <summary>
    /// Prüft, dass die Auswahl ein Foto begrenzt und das Original ohne Komprimierung anfordert.
    /// </summary>
    [Fact]
    public async Task Pick_LimitsToOnePhotoWithoutCompression()
    {
        _media.PickResults = [new FileResult(Path.Combine(Path.GetTempPath(), "beleg.heic"))];

        var photo = await CreatePicker().PickAsync();

        Assert.Equal(".heic", photo!.Extension);
        Assert.Equal(1, _media.PickOptions?.SelectionLimit);
        Assert.Equal(100, _media.PickOptions?.CompressionQuality);
    }

    /// <summary>
    /// Prüft, dass ein Abbruch (leere Liste bzw. kein Ergebnis) <see langword="null"/> liefert und nicht ohne Kamera aufgenommen wird.
    /// </summary>
    [Fact]
    public async Task Cancel_And_NoCamera_ReturnNull()
    {
        _media.PickResults = [];
        Assert.Null(await CreatePicker().PickAsync());
        Assert.Null(await CreatePicker().CaptureAsync());

        var noCamera = new FakeMediaPicker { IsCaptureSupported = false, CaptureResult = new FileResult(Path.Combine(Path.GetTempPath(), "beleg.jpg")) };
        Assert.Null(await new MauiReceiptPhotoPicker(noCamera).CaptureAsync());
        Assert.Null(noCamera.CaptureOptions);
    }

    /// <summary>
    /// Prüft die Abbildung von <see cref="PermissionException"/> und <see cref="FeatureNotSupportedException"/> bei Aufnahme und Auswahl.
    /// </summary>
    /// <param name="capture"><see langword="true"/> für die Aufnahme.</param>
    /// <param name="permission"><see langword="true"/> für <see cref="PermissionException"/>, sonst <see cref="FeatureNotSupportedException"/>.</param>
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task PlatformExceptions_AreMapped(bool capture, bool permission)
    {
        Exception inner = permission ? new PermissionException("Kamera") : new FeatureNotSupportedException("Kamera");
        _media.Exception = inner;
        var picker = CreatePicker();

        var ex = await Assert.ThrowsAsync<ReceiptPhotoAccessException>(() => capture ? picker.CaptureAsync() : picker.PickAsync());

        Assert.Equal(permission ? ReceiptPhotoAccessProblem.PermissionDenied : ReceiptPhotoAccessProblem.NotSupported, ex.Problem);
        Assert.Same(inner, ex.InnerException);
    }

    /// <summary>
    /// Prüft, dass sonstige Ausnahmen und Abbrüche unverändert weitergereicht werden.
    /// </summary>
    [Fact]
    public async Task OtherExceptions_PassThrough()
    {
        var picker = CreatePicker();

        _media.Exception = new IOException("kaputt");
        await Assert.ThrowsAsync<IOException>(() => picker.CaptureAsync());

        _media.Exception = new OperationCanceledException();
        await Assert.ThrowsAsync<OperationCanceledException>(() => picker.PickAsync());
    }
}
