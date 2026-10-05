using FlaUI.Core.AutomationElements;
using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Basis für E2E-Tests der Karte „Datenquelle“: startet die App im Testmodus mit der Adresse eines Preisdienstes
/// (Mock-Server oder bewusst unerreichbarer Port) und einem Test-Schlüssel; produktive Endpunkte werden nie angesprochen.
/// </summary>
public abstract class PriceServiceE2ETestBase : SettingsE2ETestBase
{
    private readonly MockTankerkoenigServer? _server;

    /// <summary>
    /// Startet die App mit der angegebenen Preisdienst-Adresse.
    /// </summary>
    /// <param name="baseUrl">Adresse des Preisdienstes.</param>
    /// <param name="server">Der zugehörige Mock-Server (wird nach dem Test beendet) oder <see langword="null"/>.</param>
    protected PriceServiceE2ETestBase(Uri baseUrl, MockTankerkoenigServer? server)
        : base(new Dictionary<string, string>
        {
            [TestDataPaths.PriceApiUrlEnvironmentVariable] = baseUrl.ToString(),
            [TestDataPaths.PriceApiKeyEnvironmentVariable] = MockTankerkoenigServer.AcceptedKey,
        })
    {
        _server = server;
    }

    /// <summary>
    /// Wechselt auf die Optionen-Seite, löst die Prüfung des Preisdienstes aus und liefert den angezeigten Status.
    /// </summary>
    /// <returns>Der Text der Statusmeldung.</returns>
    protected string RunServiceCheck()
    {
        OpenSettings();
        var button = WaitForAutomationId("Settings.PriceService.Check");
        button.Patterns.Invoke.Pattern.Invoke();

        string? text = null;
        WaitUntil(
            () =>
            {
                text = FindStatusText();
                return text is { Length: > 0 } && !text.Contains('…');
            },
            "Die Prüfung des Preisdienstes lieferte keinen Status.");
        return text!;
    }

    /// <summary>
    /// Anzahl der Anfragen, die der Mock-Server erhalten hat.
    /// </summary>
    protected int MockRequestCount => _server?.TotalRequests ?? 0;

    /// <inheritdoc />
    protected override void Cleanup()
    {
        _server?.Dispose();
    }

    private string? FindStatusText()
    {
        AutomationElement? element = MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("Settings.PriceService.Status"));
        return element?.Name;
    }
}
