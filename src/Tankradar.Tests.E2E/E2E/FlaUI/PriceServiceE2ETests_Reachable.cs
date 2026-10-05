using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Tests: Die Quellenangabe ist in den Optionen sichtbar, und die Prüfung des Preisdienstes gelingt gegen den Mock-Server.
/// </summary>
public class PriceServiceE2ETests_Reachable : PriceServiceE2ETestBase
{
    /// <summary>
    /// Startet die App gegen einen lokalen Mock-Server.
    /// </summary>
    public PriceServiceE2ETests_Reachable()
        : this(new MockTankerkoenigServer())
    {
    }

    private PriceServiceE2ETests_Reachable(MockTankerkoenigServer server)
        : base(server.BaseUrl, server)
    {
    }

    /// <summary>
    /// Prüft, dass die Quellenangabe „Daten: Tankerkönig / MTS-K“ in den Optionen sichtbar ist.
    /// </summary>
    [Fact]
    public void Options_ShowSourceAttribution()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();

            var attribution = WaitForAutomationId("Settings.Attribution");
            Assert.Equal("Daten: Tankerkönig / MTS-K", attribution.Name);
        });
    }

    /// <summary>
    /// Prüft, dass die Verbindungsprüfung den Mock-Dienst erreicht und den hinterlegten Schlüssel anzeigt.
    /// </summary>
    [Fact]
    public void ServiceCheck_AgainstMock_ReportsReachable()
    {
        RunWithDiagnostics(() =>
        {
            var status = RunServiceCheck();

            Assert.Equal("Der Preisdienst ist erreichbar.", status);
            Assert.True(MockRequestCount >= 1, "Der Mock-Server hat keine Anfrage erhalten.");
            Assert.Equal("API-Schlüssel: hinterlegt", WaitForAutomationId("Settings.PriceService.KeyStatus").Name);
        });
    }
}
