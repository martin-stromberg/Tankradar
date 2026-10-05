using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Fehlerfälle der Adresssuche (nicht auffindbarer Ort, ungültige Eingabe, nicht erreichbarer Ortssuchdienst) und Einhaltung der Anfragebegrenzung von Nominatim.
/// </summary>
public class SearchE2ETests_AddressErrors : SearchE2ETestBase
{
    /// <summary>
    /// Prüft, dass ein unbekannter Ort verständlich gemeldet wird und keine Preisanfrage erfolgt.
    /// </summary>
    [Fact]
    public void UnknownPlace_ShowsNotFoundMessageWithoutPriceRequest()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            SelectAddressMode();
            SetAddress(MockNominatimServer.QueryUnknown);

            Submit();

            WaitForStatusMessage("Zu dieser Eingabe wurde in Deutschland kein Ort gefunden. Bitte die Schreibweise prüfen oder genauer angeben.");
            Assert.False(Exists("Search.Station.Name"));
            Assert.False(Exists("Search.EmptyState"));
            Assert.Equal(1, Geocoding.Requests);
            Assert.Equal(0, Server.TotalRequests);
        });
    }

    /// <summary>
    /// Prüft, dass ungültige Eingaben vor dem Dienstaufruf abgewiesen werden: weder Ortssuch- noch Preisdienst erhalten eine Anfrage.
    /// </summary>
    [Fact]
    public void InvalidInput_ShowsHintAndSendsNoRequest()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            SelectAddressMode();

            Submit();
            WaitForStatusMessage("Bitte eine Adresse, einen Ort oder eine Postleitzahl eingeben.");

            SetAddress("ab");
            Submit();
            WaitForStatusMessage("Die Eingabe ist zu kurz. Bitte mindestens 3 Zeichen eingeben.");

            SetAddress("Berlin<b>");
            Submit();
            WaitForStatusMessage("Die Eingabe enthält ungültige Zeichen. Erlaubt sind Buchstaben, Ziffern und gängige Satzzeichen.");

            Assert.Equal(0, Geocoding.Requests);
            Assert.Equal(0, Server.TotalRequests);
        });
    }

    /// <summary>
    /// Prüft, dass ein nicht erreichbarer Ortssuchdienst verständlich gemeldet wird.
    /// </summary>
    [Fact]
    public void UnreachableGeocoding_ShowsServiceMessage()
    {
        RunWithDiagnostics(() =>
        {
            RestartApplication(CreateEnvironment(Server.BaseUrl, TestLocation, MockTankerkoenigServer.AcceptedKey, ClosedPortUrl()));
            OpenSearch();
            SelectAddressMode();
            SetAddress("Berlin");

            Submit();

            WaitForStatusMessage("Der Ortssuchdienst von OpenStreetMap ist nicht erreichbar. Bitte versuche es später erneut.");
            Assert.Equal(0, Server.TotalRequests);
        });
    }

    /// <summary>
    /// Prüft, dass zwei kurz hintereinander abgesendete Suchen höchstens eine Anfrage je Sekunde an Nominatim ergeben.
    /// </summary>
    [Fact]
    public void TwoQuickSearches_RespectOneRequestPerSecond()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            SelectAddressMode();
            SetRadius("1");
            SetAddress("Oranienburg");
            Submit();
            WaitForStationNames("Epsilon Tankstelle");

            SetAddress("Eberswalde");
            Submit();
            WaitForStationNames("Delta Tankstelle");

            var times = Geocoding.RequestTimes;
            Assert.Equal(2, times.Count);
            Assert.True(times[1] - times[0] >= TimeSpan.FromMilliseconds(950), $"Abstand nur {(times[1] - times[0]).TotalMilliseconds} ms.");
        });
    }
}
