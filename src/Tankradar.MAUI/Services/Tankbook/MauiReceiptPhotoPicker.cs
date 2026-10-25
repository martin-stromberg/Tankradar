using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Umsetzung von <see cref="IReceiptPhotoPicker"/> über <see cref="MediaPicker.Default"/>. Das Foto wird nicht verkleinert oder neu kodiert.
/// </summary>
public sealed class MauiReceiptPhotoPicker : IReceiptPhotoPicker
{
    /// <inheritdoc />
    public bool IsCaptureSupported
    {
        get { return MediaPicker.Default.IsCaptureSupported; }
    }

    /// <inheritdoc />
    public async Task<PickedReceiptPhoto?> PickAsync(CancellationToken cancellationToken = default)
    {
        // PickPhotoAsync ist in .NET MAUI 10 veraltet; die Mehrfachauswahl wird auf ein Foto begrenzt.
        return await TranslateAsync(async () =>
        {
            var results = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 }).ConfigureAwait(true);
            return ToPhoto(results?.FirstOrDefault());
        }).ConfigureAwait(true);
    }

    /// <inheritdoc />
    public async Task<PickedReceiptPhoto?> CaptureAsync(CancellationToken cancellationToken = default)
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            return null;
        }

        return await TranslateAsync(async () =>
        {
            var result = await MediaPicker.Default.CapturePhotoAsync().ConfigureAwait(true);
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
