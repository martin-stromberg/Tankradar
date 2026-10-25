namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Testdouble für <see cref="IMediaPicker"/>: liefert vorgegebene Ergebnisse oder löst eine vorgegebene Ausnahme aus und merkt sich die übergebenen Optionen.
/// </summary>
public sealed class FakeMediaPicker : IMediaPicker
{
    /// <inheritdoc />
    public bool IsCaptureSupported { get; set; } = true;

    /// <summary>
    /// Die Ausnahme, die jeder Aufruf auslöst; <see langword="null"/> löst keine aus.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Das Ergebnis von <see cref="CapturePhotoAsync"/>.
    /// </summary>
    public FileResult? CaptureResult { get; set; }

    /// <summary>
    /// Die Ergebnisse von <see cref="PickPhotosAsync"/>.
    /// </summary>
    public List<FileResult>? PickResults { get; set; }

    /// <summary>
    /// Die zuletzt an <see cref="CapturePhotoAsync"/> übergebenen Optionen.
    /// </summary>
    public MediaPickerOptions? CaptureOptions { get; private set; }

    /// <summary>
    /// Die zuletzt an <see cref="PickPhotosAsync"/> übergebenen Optionen.
    /// </summary>
    public MediaPickerOptions? PickOptions { get; private set; }

    /// <inheritdoc />
    public Task<FileResult?> CapturePhotoAsync(MediaPickerOptions? options = null)
    {
        CaptureOptions = options;
        return Exception is { } exception ? Task.FromException<FileResult?>(exception) : Task.FromResult(CaptureResult);
    }

    /// <inheritdoc />
    public Task<List<FileResult>> PickPhotosAsync(MediaPickerOptions? options = null)
    {
        PickOptions = options;
        return Exception is { } exception ? Task.FromException<List<FileResult>>(exception) : Task.FromResult(PickResults!);
    }

    /// <inheritdoc />
    public Task<FileResult?> PickPhotoAsync(MediaPickerOptions? options = null)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<FileResult?> CaptureVideoAsync(MediaPickerOptions? options = null)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<FileResult?> PickVideoAsync(MediaPickerOptions? options = null)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<List<FileResult>> PickVideosAsync(MediaPickerOptions? options = null)
    {
        throw new NotSupportedException();
    }
}
