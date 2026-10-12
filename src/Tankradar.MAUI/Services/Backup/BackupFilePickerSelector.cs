using Tankradar.TestSupport;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Wählt die Dateiauswahl: Im Testmodus (<c>TANKATLAS_TEST_DATA_PATH</c> gesetzt) ausschließlich die Testauswahl mit der Datei aus
/// <c>TANKATLAS_TEST_BACKUP_IMPORT</c>, sonst die Plattformauswahl. Die echte Plattform wird im Testmodus nie erzeugt.
/// </summary>
public static class BackupFilePickerSelector
{
    /// <summary>
    /// Erstellt die passende Dateiauswahl.
    /// </summary>
    /// <param name="getEnvironmentVariable">Liefert Umgebungsvariablen.</param>
    /// <param name="createPlatformPicker">Erzeugt die Plattformauswahl.</param>
    /// <returns>Die Dateiauswahl.</returns>
    public static IBackupFilePicker Create(Func<string, string?> getEnvironmentVariable, Func<IBackupFilePicker> createPlatformPicker)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        ArgumentNullException.ThrowIfNull(createPlatformPicker);

        return string.IsNullOrWhiteSpace(getEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable))
            ? createPlatformPicker()
            : new TestBackupFilePicker(getEnvironmentVariable(TestDataPaths.TestBackupImportEnvironmentVariable));
    }
}
