using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Fehlerfälle der Suche (Standort nicht verfügbar, Fehler des Preisdienstes) zeigen verständliche Meldungen statt eines leeren Ergebnisses.
/// </summary>
public class SearchE2ETests_Errors : SearchE2ETestBase
{
    /// <summary>
    /// Prüft, dass ohne gültigen Teststandort ein Hinweis erscheint, die Liste leer bleibt und der Preisdienst nicht angefragt wird.
    /// </summary>
    /// <param name="location">Der Wert für den Teststandort; <see langword="null"/> lässt die Variable weg.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("kein-standort")]
    public void LocationUnavailable_ShowsHintAndSendsNoRequest(string? location)
    {
        RunWithDiagnostics(() =>
        {
            RestartApplication(CreateEnvironment(Server.BaseUrl, location, MockTankerkoenigServer.AcceptedKey));
            OpenSearch();

            Submit();

            WaitForStatusMessage("Der Standort konnte nicht ermittelt werden. Bitte versuche es später erneut.");
            Assert.False(Exists("Search.Station.Name"));
            Assert.False(Exists("Search.EmptyState"));
            Assert.Equal(0, Server.TotalRequests);
        });
    }

    /// <summary>
    /// Prüft, dass ein Serverfehler bei leerem Speicher eine Fehlermeldung statt „Keine Tankstellen im Umkreis gefunden“ zeigt.
    /// </summary>
    [Fact]
    public void ServiceError_WithEmptyCache_ShowsFailureInsteadOfEmptyState()
    {
        RunWithDiagnostics(() =>
        {
            Server.EnqueueStatuses(500, 500, 500);
            OpenSearch();

            Submit();

            WaitForStatusMessage("Der Preisdienst ist nicht erreichbar.");
            Assert.False(Exists("Search.EmptyState"));
            Assert.False(Exists("Search.Station.Name"));
        });
    }

    /// <summary>
    /// Prüft, dass ein abgelehnter Schlüssel eine verständliche Meldung ohne technische Details zeigt.
    /// </summary>
    [Fact]
    public void RejectedKey_ShowsUnderstandableMessage()
    {
        RunWithDiagnostics(() =>
        {
            RestartApplication(CreateEnvironment(Server.BaseUrl, TestLocation, "falscher-test-schluessel"));
            OpenSearch();

            Submit();

            WaitForStatusMessage("Der Preisdienst hat die Anfrage abgelehnt. Bitte den API-Schlüssel prüfen.");
            Assert.False(Exists("Search.EmptyState"));
        });
    }
}
