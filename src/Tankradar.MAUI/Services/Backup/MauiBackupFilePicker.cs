using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Umsetzung von <see cref="IBackupFilePicker"/> über <see cref="FilePicker.Default"/> mit dem Dateityp ZIP je Plattform.
/// </summary>
public sealed class MauiBackupFilePicker : IBackupFilePicker
{
    private static readonly FilePickerFileType ZipType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.WinUI, new[] { ".zip" } },
        { DevicePlatform.iOS, new[] { "public.zip-archive" } },
        { DevicePlatform.MacCatalyst, new[] { "public.zip-archive" } },
        { DevicePlatform.Android, new[] { "application/zip" } },
    });

    /// <inheritdoc />
    public async Task<PickedBackupFile?> PickAsync(CancellationToken cancellationToken = default)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = BackupTexts.PickerTitle, FileTypes = ZipType }).ConfigureAwait(true);
        return result is null ? null : new PickedBackupFile(result.FileName, result.OpenReadAsync);
    }
}
