using Tankradar.MAUI.Models.Backup;
using Tankradar.TestSupport;

namespace Tankradar.MAUI.Services.Diagnostics;

/// <summary>
/// Wählt die Weitergabe: Im Testmodus (<c>TANKATLAS_TEST_DATA_PATH</c> gesetzt) ausschließlich die Testweitergabe in <c>&lt;Daten&gt;/test-shared</c>,
/// sonst die Plattformfunktion. Die echte Plattform wird im Testmodus nie erzeugt.
/// </summary>
public static class DiagnosticsSharerSelector
{
    /// <summary>
    /// Erstellt die passende Weitergabe.
    /// </summary>
    /// <param name="getEnvironmentVariable">Liefert Umgebungsvariablen.</param>
    /// <param name="createPlatformSharer">Erzeugt die Plattformweitergabe.</param>
    /// <returns>Die Weitergabe.</returns>
    public static IDiagnosticsSharer Create(Func<string, string?> getEnvironmentVariable, Func<IDiagnosticsSharer> createPlatformSharer)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        ArgumentNullException.ThrowIfNull(createPlatformSharer);

        var dataPath = getEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable);
        return string.IsNullOrWhiteSpace(dataPath)
            ? createPlatformSharer()
            : new TestDiagnosticsSharer(Path.Combine(dataPath, BackupFormat.TestSharedDirectoryName));
    }
}
