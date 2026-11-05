using FlaUI.Core.Definitions;
using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Der OpenRouteService-Schlüssel wird in den Optionen eingetragen (Testmodus: Feld unverdeckt, Bedienung per UI-Automation-Muster), per Testanfrage gegen den Mock geprüft,
/// und erst mit gespeichertem Schlüssel lässt sich die Routensuche aktivieren. Nur dann ist die Suchart „Route“ sichtbar; ohne Aktivierung geht kein Aufruf an den Routingdienst.
/// </summary>
public class RouteE2ETests_Settings : RouteE2ETestBase
{
    /// <summary>
    /// Startet die App ohne Schlüssel für den Routingdienst.
    /// </summary>
    public RouteE2ETests_Settings()
        : base(routingKey: null)
    {
    }

    private bool IsSwitchEnabled()
    {
        return WaitForAutomationId("Settings.Route.Switch").Properties.IsEnabled.ValueOrDefault;
    }

    private bool IsSwitchOn()
    {
        return WaitForAutomationId("Settings.Route.Switch").Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On;
    }

    private void Invoke(string automationId)
    {
        WaitForAutomationId(automationId).Patterns.Invoke.Pattern.Invoke();
    }

    private void ToggleSwitch()
    {
        WaitForAutomationId("Settings.Route.Switch").Patterns.Toggle.Pattern.Toggle();
    }

    private void WaitForRouteStatus(string expectedStart)
    {
        var last = string.Empty;
        WaitUntil(
            () =>
            {
                last = FindByAutomationId("Settings.Route.Status")?.Name ?? string.Empty;
                return last.StartsWith(expectedStart, StringComparison.Ordinal);
            },
            $"Die Rückmeldung der Routensuche beginnt nicht mit \"{expectedStart}\", zuletzt: \"{last}\".");
    }

    private void SaveKeyAndEnable()
    {
        OpenSettings();
        SetFieldValue("Settings.Route.KeyInput", MockOpenRouteServiceServer.AcceptedKey);
        Invoke("Settings.Route.Save");
        WaitUntil(IsSwitchEnabled, "Der Schalter wurde trotz gespeichertem Schlüssel nicht bedienbar.");
        ToggleSwitch();
        WaitUntil(IsSwitchOn, "Der Schalter für die Routensuche wurde nicht eingeschaltet.");
    }

    /// <summary>
    /// Prüft, dass ohne Schlüssel der Schalter gesperrt und aus ist, der Hinweis erscheint und die Suchart „Route“ fehlt, ohne dass etwas an den Routingdienst geht.
    /// </summary>
    [Fact]
    public void WithoutKey_SwitchIsLockedAndRouteModeHidden()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();

            Assert.Equal("Schlüssel: nicht gespeichert", WaitForAutomationId("Settings.Route.KeyStatus").Name);
            Assert.False(IsSwitchEnabled());
            Assert.False(IsSwitchOn());
            Assert.True(Exists("Settings.Route.EnableHint"));
            Assert.False(Exists("Settings.Route.Remove"));

            OpenSearch();
            OpenSearchOverlay();
            Assert.True(Exists("Search.Mode.Address"));
            Assert.False(Exists("Search.Mode.Route"));
            Assert.Equal(0, Routing.Requests);
        });
    }

    /// <summary>
    /// Prüft den ganzen Ablauf: falschen Schlüssel prüfen (abgelehnt), richtigen prüfen (gültig, noch nicht gespeichert), speichern, aktivieren, Modus erscheint, Suche funktioniert.
    /// </summary>
    [Fact]
    public void EnterCheckSaveEnable_ShowsRouteModeAndSearchWorks()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();

            SetFieldValue("Settings.Route.KeyInput", "falscher-schluessel");
            Invoke("Settings.Route.Check");
            WaitForRouteStatus("Der Schlüssel wurde abgelehnt");
            Assert.Equal(1, Routing.Requests);
            Assert.False(IsSwitchEnabled());

            SetFieldValue("Settings.Route.KeyInput", MockOpenRouteServiceServer.AcceptedKey);
            Invoke("Settings.Route.Check");
            WaitForRouteStatus("Der Schlüssel ist gültig. Er ist noch nicht gespeichert.");
            Assert.Equal(2, Routing.Requests);
            Assert.Equal(MockOpenRouteServiceServer.AcceptedKey, Routing.LastAuthorization);
            Assert.False(IsSwitchEnabled());

            Invoke("Settings.Route.Save");
            WaitForRouteStatus("Der Schlüssel wurde gespeichert.");
            WaitUntil(IsSwitchEnabled, "Der Schalter wurde trotz gespeichertem Schlüssel nicht bedienbar.");
            Assert.Equal("Schlüssel: gespeichert", WaitForAutomationId("Settings.Route.KeyStatus").Name);
            Assert.Equal(string.Empty, WaitForAutomationId("Settings.Route.KeyInput").Patterns.Value.Pattern.Value.Value);
            Assert.False(IsSwitchOn());

            OpenSearch();
            OpenSearchOverlay();
            Assert.False(Exists("Search.Mode.Route"));
            OpenSettings();
            ToggleSwitch();
            WaitUntil(IsSwitchOn, "Der Schalter für die Routensuche wurde nicht eingeschaltet.");

            OpenRouteSearch();
            FillRoute("10115", "Oranienburg");
            Submit();
            WaitForStationNames(TwoKilometerStations);
            Assert.Equal(3, Routing.Requests);
            Assert.Equal(MockOpenRouteServiceServer.AcceptedKey, Routing.LastAuthorization);
        });
    }

    /// <summary>
    /// Prüft, dass das Entfernen des Schlüssels die Routensuche ausschaltet und den Modus „Route“ wieder ausblendet.
    /// </summary>
    [Fact]
    public void RemovingKey_DisablesRouteSearchAndHidesMode()
    {
        RunWithDiagnostics(() =>
        {
            SaveKeyAndEnable();
            OpenSearch();
            OpenSearchOverlay();
            Assert.True(Exists("Search.Mode.Route"));

            OpenSettings();
            Invoke("Settings.Route.Remove");
            WaitForRouteStatus("Der Schlüssel wurde entfernt.");

            WaitUntil(() => !IsSwitchEnabled() && !IsSwitchOn(), "Der Schalter ist nach dem Entfernen nicht gesperrt und aus.");
            WaitUntil(() => !Exists("Settings.Route.Remove"), "Die Schaltfläche zum Entfernen blieb sichtbar.");
            OpenSearch();
            OpenSearchOverlay();
            Assert.False(Exists("Search.Mode.Route"));
            Assert.Equal(0, Routing.Requests);
        });
    }

    /// <summary>
    /// Prüft, dass das Ausschalten bei vorhandenem Schlüssel den Modus „Route“ ausblendet und das Wiedereinschalten ihn zurückbringt.
    /// </summary>
    [Fact]
    public void TogglingSwitch_HidesAndShowsRouteMode()
    {
        RunWithDiagnostics(() =>
        {
            SaveKeyAndEnable();
            OpenSearch();
            OpenSearchOverlay();
            SelectRouteMode();

            OpenSettings();
            ToggleSwitch();
            WaitUntil(() => !IsSwitchOn(), "Der Schalter wurde nicht ausgeschaltet.");
            OpenSearch();
            OpenSearchOverlay();
            Assert.False(Exists("Search.Mode.Route"));
            Assert.True(Exists("Search.Radius.5"));

            OpenSettings();
            ToggleSwitch();
            WaitUntil(IsSwitchOn, "Der Schalter wurde nicht wieder eingeschaltet.");
            OpenSearch();
            OpenSearchOverlay();
            Assert.True(Exists("Search.Mode.Route"));
            Assert.Equal(0, Routing.Requests);
        });
    }
}
