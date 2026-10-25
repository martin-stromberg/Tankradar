using System.Text;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Testdouble für <see cref="IReceiptPhotoPicker"/>: liefert ein Foto mit vorgegebener Endung oder <see langword="null"/> (Abbruch).
/// </summary>
public sealed class FakeReceiptPhotoPicker : IReceiptPhotoPicker
{
    /// <inheritdoc />
    public bool IsCaptureSupported { get; set; }

    /// <summary>
    /// Die Endung des nächsten gelieferten Fotos; <see langword="null"/> simuliert den Abbruch durch den Nutzer.
    /// </summary>
    public string? NextExtension { get; set; } = ".jpg";

    /// <summary>
    /// Hält die nächste Auswahl an, bis das Tor geöffnet wird (simuliert einen offenen Systemdialog); <see langword="null"/> liefert sofort.
    /// </summary>
    public TaskCompletionSource? Gate { get; set; }

    /// <summary>
    /// Die Ausnahme, die jeder Aufruf auslöst, bis sie auf <see langword="null"/> zurückgesetzt wird (z. B. <see cref="ReceiptPhotoAccessException"/>).
    /// </summary>
    public Exception? NextException { get; set; }

    /// <summary>
    /// Die Zahl der Aufrufe von <see cref="PickAsync"/>.
    /// </summary>
    public int PickCalls { get; private set; }

    /// <summary>
    /// Die Zahl der Aufrufe von <see cref="CaptureAsync"/>.
    /// </summary>
    public int CaptureCalls { get; private set; }

    /// <inheritdoc />
    public async Task<PickedReceiptPhoto?> PickAsync(CancellationToken cancellationToken = default)
    {
        PickCalls++;
        if (NextException is { } exception)
        {
            throw exception;
        }

        if (Gate is { } gate)
        {
            await gate.Task;
        }

        return Create();
    }

    /// <inheritdoc />
    public Task<PickedReceiptPhoto?> CaptureAsync(CancellationToken cancellationToken = default)
    {
        CaptureCalls++;
        if (NextException is { } exception)
        {
            return Task.FromException<PickedReceiptPhoto?>(exception);
        }

        return Task.FromResult(Create());
    }

    private PickedReceiptPhoto? Create()
    {
        return NextExtension is { } extension
            ? new PickedReceiptPhoto(extension, () => Task.FromResult<Stream>(new MemoryStream(Encoding.ASCII.GetBytes("photo"))))
            : null;
    }
}
