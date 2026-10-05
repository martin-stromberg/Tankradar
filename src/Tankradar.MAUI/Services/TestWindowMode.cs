using Tankradar.TestSupport;

namespace Tankradar.MAUI.Services;

/// <summary>
/// Entscheidet, ob die App ihr Fenster im Testmodus außerhalb des sichtbaren Bildschirmbereichs und ohne Vordergrundwechsel startet.
/// Außerhalb des Testmodus (<c>TANKATLAS_TEST_DATA_PATH</c> nicht gesetzt) wirkt die Einstellung nie.
/// </summary>
public static class TestWindowMode
{
    /// <summary>
    /// Liefert, ob das Fenster unsichtbar (außerhalb des Bildschirms, ohne Aktivierung) gestartet werden soll.
    /// </summary>
    /// <param name="getEnvironmentVariable">Liefert Umgebungsvariablen.</param>
    /// <returns><see langword="true"/> nur im Testmodus mit <c>TANKATLAS_TEST_WINDOW=offscreen</c>.</returns>
    public static bool ShouldHideWindow(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(getEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable)))
        {
            return false;
        }

        return string.Equals(
            getEnvironmentVariable(TestDataPaths.TestWindowEnvironmentVariable)?.Trim(),
            TestDataPaths.TestWindowOffscreen,
            StringComparison.OrdinalIgnoreCase);
    }
}
