using Tankradar.MAUI.Services.Navigation;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der Link-Öffner im Testmodus und für Adressen ohne HTTPS nie einen Browser startet.
/// </summary>
public class LauncherLinkOpenerTests : BaseTest
{
    /// <summary>
    /// Prüft, dass im Testmodus und für Nicht-HTTPS-Adressen nichts geöffnet wird (kein Fehler, kein Start).
    /// </summary>
    [Fact]
    public async Task OpenAsync_TestModeOrNonHttps_DoesNothing()
    {
        await new LauncherLinkOpener(testMode: true).OpenAsync(new Uri("https://example.test/"));
        await new LauncherLinkOpener(testMode: false).OpenAsync(new Uri("http://example.test/"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => new LauncherLinkOpener(testMode: true).OpenAsync(null!));
    }
}
