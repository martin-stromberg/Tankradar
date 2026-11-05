using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Fotoauswahl für den Testmodus: liefert ohne Systemdialog die Datei aus <c>TANKATLAS_TEST_RECEIPT_PHOTO</c>; ohne vorhandene Datei gilt die Auswahl als abgebrochen.
/// Aufnahmen mit der Kamera gibt es im Testmodus nicht.
/// </summary>
public sealed class TestReceiptPhotoPicker : IReceiptPhotoPicker
{
    private readonly string? _filePath;

    /// <summary>
    /// Erstellt die Auswahl.
    /// </summary>
    /// <param name="filePath">Der Pfad der gelieferten Datei oder <see langword="null"/>.</param>
    public TestReceiptPhotoPicker(string? filePath)
    {
        _filePath = filePath;
    }

    /// <inheritdoc />
    public bool IsCaptureSupported
    {
        get { return false; }
    }

    /// <inheritdoc />
    public Task<PickedReceiptPhoto?> PickAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath))
        {
            return Task.FromResult<PickedReceiptPhoto?>(null);
        }

        var path = _filePath;
        return Task.FromResult<PickedReceiptPhoto?>(new PickedReceiptPhoto(Path.GetExtension(path), () => Task.FromResult<Stream>(File.OpenRead(path))));
    }

    /// <inheritdoc />
    public Task<PickedReceiptPhoto?> CaptureAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<PickedReceiptPhoto?>(null);
    }
}
