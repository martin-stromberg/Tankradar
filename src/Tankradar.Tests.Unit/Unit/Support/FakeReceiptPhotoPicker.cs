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
    /// Die Zahl der Aufrufe von <see cref="CaptureAsync"/>.
    /// </summary>
    public int CaptureCalls { get; private set; }

    /// <inheritdoc />
    public Task<PickedReceiptPhoto?> PickAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Create());
    }

    /// <inheritdoc />
    public Task<PickedReceiptPhoto?> CaptureAsync(CancellationToken cancellationToken = default)
    {
        CaptureCalls++;
        return Task.FromResult(Create());
    }

    private PickedReceiptPhoto? Create()
    {
        return NextExtension is { } extension
            ? new PickedReceiptPhoto(extension, () => Task.FromResult<Stream>(new MemoryStream(Encoding.ASCII.GetBytes("photo"))))
            : null;
    }
}
