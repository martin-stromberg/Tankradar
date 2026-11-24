using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Liefert der Preisdienst eine übergroße Antwort, meldet die Verbindungsprüfung eine unerwartete Antwort.
/// </summary>
public class PriceServiceE2ETests_Oversized : PriceServiceE2ETestBase
{
    /// <summary>
    /// Startet die App gegen einen lokalen Mock-Server, der jede Antwort über das zulässige Größenlimit auffüllt.
    /// </summary>
    public PriceServiceE2ETests_Oversized()
        : this(new MockTankerkoenigServer())
    {
    }

    private PriceServiceE2ETests_Oversized(MockTankerkoenigServer server)
        : base(server.BaseUrl, server)
    {
        server.ResponsePaddingBytes = 2 * 1024 * 1024;
    }

    /// <summary>
    /// Prüft, dass die Prüfung eine übergroße Antwort kontrolliert als „unerwartete Antwort“ meldet.
    /// </summary>
    [Fact]
    public void ServiceCheck_WithOversizedResponse_ReportsUnexpectedAnswer()
    {
        RunWithDiagnostics(() =>
        {
            var status = RunServiceCheck();

            Assert.Equal("Der Preisdienst hat eine unerwartete Antwort geliefert.", status);
        });
    }
}
