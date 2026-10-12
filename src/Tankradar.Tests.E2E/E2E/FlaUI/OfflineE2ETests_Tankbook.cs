using System.Globalization;
using Microsoft.Data.Sqlite;
using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// FlaUI-Offline-Szenario: Der Mock-Preisdienst ist nach einem Neustart nicht erreichbar. Startseite und Detailansicht zeigen die zwischengespeicherten Preise
/// mit Altersangabe und Offline-Hinweis; das Tankbuch bleibt voll nutzbar (Fahrzeug anlegen, Vorgang mit bekannter Tankstelle erfassen, Verbrauch ansehen,
/// Vorgang korrigieren und löschen).
/// </summary>
public class OfflineE2ETests_Tankbook : HomeE2ETestBase
{
    /// <summary>
    /// Erstellt den Test mit einem Treiber für das Tankbuch.
    /// </summary>
    public OfflineE2ETests_Tankbook()
    {
        Tankbook = new TankbookE2EDriver(() => MainWindow, SetFieldValue, WaitUntil, NavigateToTab);
    }

    private TankbookE2EDriver Tankbook { get; }

    private string DatabasePath
    {
        get { return Path.Combine(TestDataDirectory, "tankatlas.db"); }
    }

    private void AgeStationDetails()
    {
        using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False;Default Timeout=5");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Stations SET DetailsUpdatedUtc = $aged";
        command.Parameters.AddWithValue("$aged", DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Prüft das vollständige Offline-Szenario.
    /// </summary>
    [Fact]
    public void UnreachableService_PricesFromCacheAndTankbookFullyUsable()
    {
        RunWithDiagnostics(() =>
        {
            CreateGroupWithStation(0, "Alpha Tankstelle", "Arbeitsweg");

            RestartApplication(CreateEnvironment(ClosedPortUrl(), TestLocation, MockTankerkoenigServer.AcceptedKey));

            // Die Detailansicht nutzt Angaben jünger als 5 Minuten ohne Abruf; älter gemachte Details erzwingen den (scheiternden) Abruf beim Öffnen.
            AgeStationDetails();
            OpenHome("Arbeitsweg");
            Assert.Equal("Offline: keine Verbindung zum Preisdienst.", WaitForAutomationId("Favorites.OfflineBanner").Name);
            WaitForText("Favorites.Station.Price.SuperE5", "1,859 €/L");
            Assert.All(SafeTexts("Favorites.Station.Age.SuperE5"), text => Assert.Matches(AgePattern, text));

            Press("Favorites.Station.Open");
            WaitUntil(() => FindByAutomationId("Detail.Name")?.Name == "Alpha Tankstelle", "Die Detailansicht zeigt nicht 'Alpha Tankstelle'.");
            WaitForAutomationId("Detail.OfflineBanner");
            Assert.Equal("1,859 Euro pro Liter", WaitForAutomationId("Detail.Price.SuperE5").Name);
            Assert.Matches(AgePattern, WaitForAutomationId("Detail.Age.SuperE5").Name);
            Press("NavigationViewBackButton");
            WaitForAutomationId("FavoritesPage.Headline");

            Tankbook.OpenTankbook();
            Tankbook.CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Tankbook.Press("TankbookEntries.Add");
            var chip = Tankbook.WaitFor("TankbookEntries.Form.KnownStation.0");
            Assert.Equal("Alpha Tankstelle", chip.Name);
            Tankbook.Press("TankbookEntries.Form.KnownStation.0");
            Tankbook.WaitUntil(
                () => Tankbook.Find("TankbookEntries.Form.KnownStation.0")?.Properties.HelpText.ValueOrDefault == "Ausgewählt",
                "Die bekannte Tankstelle wurde nicht ausgewählt.");
            Tankbook.WaitUntil(
                () => Tankbook.Find("TankbookEntries.Form.Station")?.Patterns.Value.PatternOrDefault?.Value.ValueOrDefault == "Alpha Tankstelle",
                "Der Name der bekannten Tankstelle wurde nicht übernommen.");
            Tankbook.FillEntryForm(string.Empty, "40", "1,859", string.Empty, "10500");
            Tankbook.SaveEntryForm();
            Tankbook.CreateEntry("Zuhause", "30", "1,859", string.Empty, "11000");
            Tankbook.WaitForTexts("TankbookEntry.Item.Consumption", "Verbrauch 6,00 L/100 km", "Noch kein Verbrauch: erster voller Vorgang");
            var stored = TankbookTestDatabase.ReadEntries(DatabasePath);
            Assert.NotNull(stored[0].StationId);
            Assert.Null(stored[1].StationId);
            Tankbook.WaitForStatistic("Fuel", "Average", "6,00");
            Tankbook.WaitForStatistic("Fuel", "CostPer100Km", "11,15");

            Tankbook.PressForEntry("TankbookEntry.Item.Edit", 0);
            Tankbook.Type("TankbookEntries.Form.Odometer", "11500");
            Tankbook.SaveEntryForm();
            Tankbook.WaitForTexts("TankbookEntry.Item.Consumption", "Verbrauch 3,00 L/100 km", "Noch kein Verbrauch: erster voller Vorgang");
            Tankbook.WaitForStatistic("Fuel", "Average", "3,00");

            Tankbook.PressForEntry("TankbookEntry.Item.Delete", 0);
            Tankbook.Press("TankbookEntry.Item.DeleteConfirm");
            Tankbook.WaitForTexts("TankbookEntry.Item.Station", "Alpha Tankstelle");
            Tankbook.WaitForStatistic("Fuel", "Average", "–");
            Assert.Equal(1, TankbookTestDatabase.CountEntries(DatabasePath));
        });
    }
}
