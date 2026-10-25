using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Umsetzung von <see cref="IReceiptPhotoPicker"/> über <see cref="IMediaPicker"/> (in der App <see cref="MediaPicker.Default"/>).
/// Die Auswahl übernimmt das Foto unverändert; die Aufnahme liefert ein von .NET MAUI als JPEG kodiertes Bild (Qualität <see cref="CaptureQuality"/>).
/// </summary>
public sealed class MauiReceiptPhotoPicker : IReceiptPhotoPicker
{
    /// <summary>
    /// Die JPEG-Qualität (in Prozent) der Aufnahme. Unter 90 erzwingt .NET MAUI auf iOS JPEG statt PNG in voller Auflösung (das kann 20 MB übersteigen).
    /// </summary>
    public const int CaptureQuality = 85;

    private readonly IMediaPicker _mediaPicker;

    /// <summary>
    /// Erstellt die Auswahl über <see cref="MediaPicker.Default"/>.
    /// </summary>
    public MauiReceiptPhotoPicker()
        : this(MediaPicker.Default)
    {
    }

    /// <summary>
    /// Erstellt die Auswahl über die angegebene Medienauswahl (für Tests austauschbar).
    /// </summary>
    /// <param name="mediaPicker">Die Medienauswahl.</param>
    public MauiReceiptPhotoPicker(IMediaPicker mediaPicker)
    {
        ArgumentNullException.ThrowIfNull(mediaPicker);
        _mediaPicker = mediaPicker;
    }

    /// <inheritdoc />
    public bool IsCaptureSupported
    {
        get { return _mediaPicker.IsCaptureSupported; }
    }

    /// <inheritdoc />
    public async Task<PickedReceiptPhoto?> PickAsync(CancellationToken cancellationToken = default)
    {
        // PickPhotoAsync ist in .NET MAUI 10 veraltet; die Mehrfachauswahl wird auf ein Foto begrenzt.
        return await TranslateAsync(async () =>
        {
            var results = await _mediaPicker.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 }).ConfigureAwait(true);
            return ToPhoto(results?.FirstOrDefault());
        }).ConfigureAwait(true);
    }

    /// <inheritdoc />
    public async Task<PickedReceiptPhoto?> CaptureAsync(CancellationToken cancellationToken = default)
    {
        if (!_mediaPicker.IsCaptureSupported)
        {
            return null;
        }

        return await TranslateAsync(async () =>
        {
            var result = await _mediaPicker.CapturePhotoAsync(new MediaPickerOptions { CompressionQuality = CaptureQuality }).ConfigureAwait(true);
            return ToPhoto(result);
        }).ConfigureAwait(true);
    }

    private static async Task<PickedReceiptPhoto?> TranslateAsync(Func<Task<PickedReceiptPhoto?>> action)
    {
        try
        {
            return await action().ConfigureAwait(true);
        }
        catch (PermissionException ex)
        {
            throw new ReceiptPhotoAccessException(ReceiptPhotoAccessProblem.PermissionDenied, ex);
        }
        catch (FeatureNotSupportedException ex)
        {
            throw new ReceiptPhotoAccessException(ReceiptPhotoAccessProblem.NotSupported, ex);
        }
    }

    private static PickedReceiptPhoto? ToPhoto(FileResult? result)
    {
        return result is null ? null : new PickedReceiptPhoto(Path.GetExtension(result.FileName), result.OpenReadAsync);
    }
}
