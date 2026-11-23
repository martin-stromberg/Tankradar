using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Host-Poll-Frist der E2E-Testbasis: mindestens 30 Sekunden, sonst das Doppelte des UIA-Timeouts, damit die
/// Wartefrist des Test-Hosts stets über dem Budget einer einzelnen UIA-Anfrage liegt.
/// </summary>
public class E2EStartupPolicyTests_HostPollTimeout : BaseTest
{
    /// <summary>
    /// Prüft die Ermittlung der Host-Poll-Frist: lokaler Standard 15 s UIA → 30 s, CI-Standard 60 s UIA → 120 s,
    /// ein kleiner Override bleibt bei der 30-s-Untergrenze, ein großer Override verdoppelt sich.
    /// </summary>
    /// <param name="explicitValue">Wert der Umgebungsvariable.</param>
    /// <param name="ci">Wert von GITHUB_ACTIONS.</param>
    /// <param name="expectedSeconds">Erwartete Frist in Sekunden.</param>
    [Theory]
    [InlineData(null, null, 30)]
    [InlineData(null, "true", 120)]
    [InlineData("1", null, 30)]
    [InlineData("600", null, 1200)]
    [InlineData("abc", "true", 120)]
    [InlineData("0", null, 30)]
    public void ResolveHostPollTimeout_UsesUiaTimeoutWithMinimum(string? explicitValue, string? ci, int expectedSeconds)
    {
        var timeout = E2EStartupPolicy.ResolveHostPollTimeout(name => name switch
        {
            E2EStartupPolicy.UiaTimeoutEnvironmentVariable => explicitValue,
            "GITHUB_ACTIONS" => ci,
            _ => null,
        });

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), timeout);
    }
}
