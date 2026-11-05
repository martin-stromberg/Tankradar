using Tankradar.MAUI.Models.Backup;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Dateiauswahl für den Testmodus: liefert ohne Systemdialog die Datei aus <c>TANKATLAS_TEST_BACKUP_IMPORT</c>; ohne vorhandene Datei gilt die Auswahl als abgebrochen.
/// </summary>
public sealed class TestBackupFilePicker : IBackupFilePicker
{
    private readonly string? _filePath;

    /// <summary>
    /// Erstellt die Auswahl.
    /// </summary>
    /// <param name="filePath">Der Pfad der gelieferten Datei oder <see langword="null"/>.</param>
    public TestBackupFilePicker(string? filePath)
    {
        _filePath = filePath;
    }

    /// <inheritdoc />
    public Task<PickedBackupFile?> PickAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath))
        {
            return Task.FromResult<PickedBackupFile?>(null);
        }

        var path = _filePath;
        return Task.FromResult<PickedBackupFile?>(new PickedBackupFile(Path.GetFileName(path), () => Task.FromResult<Stream>(File.OpenRead(path))));
    }
}
