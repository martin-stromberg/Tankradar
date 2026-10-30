namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Die Symbolschaltfläche oben links öffnet den Suchdialog über der Karte; Adress- und Routensuche laufen darüber, der Dialog schließt sich beim Start der Suche.
/// </summary>
public class MapE2ETests_SearchOverlay : MapE2ETestBase
{
    /// <summary>
    /// Prüft, dass die Symbolschaltfläche den Dialog öffnet, die Adresssuche ihn schließt und die gesuchte Position auf der Karte markiert ist.
    /// </summary>
    [Fact]
    public void OverlayButton_AddressSearch_ClosesOverlayAndMarksPosition()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            Assert.False(Exists("Search.Overlay.Close"));
            Assert.False(Exists("Search.Submit"));

            WaitForAutomationId("Search.Overlay.Open").Patterns.Invoke.Pattern.Invoke();

            Assert.Equal("Suche", WaitForAutomationId("Search.Overlay.Title").Name);
            Assert.True(Exists("Search.Mode.Address"));
            Assert.True(Exists("Search.Radius.5"));
            SelectAddressMode();
            SetAddress("Berlin");
            WaitForAutomationId("Search.Submit").Patterns.Invoke.Pattern.Invoke();

            WaitUntil(() => !Exists("Search.Overlay.Close"), "Der Suchdialog wurde beim Start der Suche nicht geschlossen.");
            WaitForMapCount("2 von 2 Stationen sichtbar");
            Assert.Equal("Gesuchte Position", WaitForAutomationId("Map.Origin").Name);
            Assert.Equal("Berlin", Geocoding.LastQuery);
        });
    }

    /// <summary>
    /// Prüft, dass das Schließen ohne Suche nichts abfragt und die Eingaben beim erneuten Öffnen erhalten bleiben.
    /// </summary>
    [Fact]
    public void CloseOverlay_KeepsInputsWithoutSearching()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            SelectAddressMode();
            SetAddress("Frankfurt");
            SetRadius("10");

            CloseSearchOverlay();

            Assert.Equal(0, Server.TotalRequests);
            Assert.Equal(0, Geocoding.Requests);
            Assert.Equal("Frankfurt", ReadAddress());
            Assert.Equal("10", ReadRadius());
            Assert.True(IsChipSelected("Search.Mode.Address"));
        });
    }

    /// <summary>
    /// Prüft die Routensuche über den Dialog (Abschnitt nur bei aktivierter Routensuche): Der Dialog schließt sich, die Karte zeigt die Route und die Trefferliste die Tankstellen entlang der Route.
    /// </summary>
    [Fact]
    public void OverlayRouteSearch_ShowsRouteOnMap()
    {
        RunWithDiagnostics(() =>
        {
            Server.AddRouteStations();
            OpenSearch();
            SelectSort("Distance");
            OpenSearchOverlay();
            Assert.False(Exists("Search.Route.Destination.Input"));

            SelectRouteMode();
            Assert.True(Exists("Search.Route.Destination.Input"));
            Assert.False(Exists("Search.Radius.5"));
            SetRouteStart("10115");
            SetRouteDestination("Oranienburg");
            WaitForAutomationId("Search.Submit").Patterns.Invoke.Pattern.Invoke();

            WaitUntil(() => !Exists("Search.Overlay.Close"), $"Der Suchdialog wurde beim Start der Routensuche nicht geschlossen ({FindByAutomationId("Search.Overlay.StatusMessage")?.Name}).");
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Route Nah", "Epsilon Tankstelle");
            WaitForAutomationId("Map.Destination");
            Assert.Equal("Routen: OpenRouteService, Geodaten © OpenStreetMap-Mitwirkende", WaitForAutomationId("Map.Attribution.Routing").Name);
            Assert.False(Exists("Map.ReloadArea"));
            Assert.Equal(1, Routing.Requests);
        });
    }
}
