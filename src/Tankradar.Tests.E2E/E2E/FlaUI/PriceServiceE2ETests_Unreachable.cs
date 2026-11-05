using System.Net;
using System.Net.Sockets;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Ist der Preisdienst nicht erreichbar (Offline-Betrieb per Testkonfiguration), bleibt die App bedienbar und meldet es verständlich.
/// </summary>
public class PriceServiceE2ETests_Unreachable : PriceServiceE2ETestBase
{
    /// <summary>
    /// Startet die App mit der Adresse eines Ports, auf dem niemand lauscht.
    /// </summary>
    public PriceServiceE2ETests_Unreachable()
        : base(ClosedPortUrl(), null)
    {
    }

    /// <summary>
    /// Prüft, dass die Prüfung „nicht erreichbar“ meldet und die Optionen weiter funktionieren.
    /// </summary>
    [Fact]
    public void ServiceCheck_WithUnreachableService_ReportsFallbackToLastKnownPrices()
    {
        RunWithDiagnostics(() =>
        {
            var status = RunServiceCheck();

            Assert.Equal("Der Preisdienst ist nicht erreichbar. Es werden die zuletzt bekannten Preise verwendet.", status);
            Assert.True(Exists("Settings.Attribution"));
            Assert.True(IsRadioChecked("Settings.Gps.WhileInUse"));
        });
    }

    private static Uri ClosedPortUrl()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return new Uri($"http://127.0.0.1:{port}/json/");
    }
}
