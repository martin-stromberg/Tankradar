namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Suche mit festem Standort und Standardradius zeigt die nahen Tankstellen mit Preisen, Alter und Hinweisen.
/// </summary>
public class SearchE2ETests_Basic : SearchE2ETestBase
{
    /// <summary>
    /// Prüft Radius-Vorbelegung, Auswahl der Tankstellen, Preise in Einstellungsreihenfolge, Altersangabe, Entfernungen und Adresszeile. Die Umkreissuche der Quelle liefert keine Öffnungszeiten, daher erscheint ohne bekannte Details kein Hinweis „Automatentankstelle“.
    /// </summary>
    [Fact]
    public void Search_DefaultRadius_ShowsNearStationsWithPricesAgeAndHints()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            Assert.Equal("5", ReadRadius());
            Assert.False(Exists("Search.OfflineBanner"));

            Submit();

            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            Assert.False(Exists("Search.StatusMessage"));
            Assert.False(Exists("Search.EmptyState"));
            Assert.Equal(["1,859 €", "1,879 €"], ReadTexts("Search.Price.SuperE5"));
            Assert.Equal(["1,799 €", "1,819 €"], ReadTexts("Search.Price.SuperE10"));
            Assert.Equal(["1,699 €"], ReadTexts("Search.Price.Diesel"));
            Assert.All(ReadTexts("Search.Age.SuperE5"), text => Assert.Equal("vor 0 Min.", text));
            var distances = ReadTexts("Search.Station.Distance");
            Assert.Equal(2, distances.Count);
            Assert.All(distances, text => Assert.EndsWith(" km", text, StringComparison.Ordinal));
            Assert.False(Exists("Search.Hint.Automated"));
            Assert.Equal(["Hauptstraße 1, 10115 Berlin", "Nebenweg 22, 10117 Berlin"], ReadTexts("Search.Station.Address"));
            Assert.Equal(["Alpha Tankstelle"], StationNamesOf("Search.Price.Diesel"));
            Assert.Equal(1, Server.ListRequests);
            Assert.Equal(5, Server.LastListRadius);
        });
    }

    /// <summary>
    /// Prüft, dass vor der ersten Suche keine Anfrage gesendet wird und weder Ergebnisse noch Leerzustand erscheinen.
    /// </summary>
    [Fact]
    public void OpeningSearch_DoesNotQueryBeforeSubmit()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();

            Assert.Equal(0, Server.TotalRequests);
            Assert.False(Exists("Search.Station.Name"));
            Assert.False(Exists("Search.EmptyState"));
        });
    }
}
