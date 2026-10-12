using Tankradar.TestSupport;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Wählt die Fotoauswahl: Im Testmodus (<c>TANKATLAS_TEST_DATA_PATH</c> gesetzt) ausschließlich die Testauswahl mit der Datei aus
/// <c>TANKATLAS_TEST_RECEIPT_PHOTO</c>, sonst die Plattformauswahl. Die echte Plattform wird im Testmodus nie erzeugt.
/// </summary>
public static class ReceiptPhotoPickerSelector
{
    /// <summary>
    /// Erstellt die passende Fotoauswahl.
    /// </summary>
    /// <param name="getEnvironmentVariable">Liefert Umgebungsvariablen.</param>
    /// <param name="createPlatformPicker">Erzeugt die Plattformauswahl.</param>
    /// <returns>Die Fotoauswahl.</returns>
    public static IReceiptPhotoPicker Create(Func<string, string?> getEnvironmentVariable, Func<IReceiptPhotoPicker> createPlatformPicker)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        ArgumentNullException.ThrowIfNull(createPlatformPicker);

        return string.IsNullOrWhiteSpace(getEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable))
            ? createPlatformPicker()
            : new TestReceiptPhotoPicker(getEnvironmentVariable(TestDataPaths.TestReceiptPhotoEnvironmentVariable));
    }
}
