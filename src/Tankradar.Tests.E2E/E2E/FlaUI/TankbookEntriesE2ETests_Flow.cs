namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test „Tankvorgang erfassen → Verbrauch wird angezeigt“: zwei Vorgänge mit zwei aus drei Werten, Liste neueste zuerst mit Strecke und Verbrauch.
/// </summary>
public class TankbookEntriesE2ETests_Flow : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft Erfassen im Tab „Tankbuch“ (Fahrzeug über die Auswahl im Kopf), Rechenhinweise, Verbrauch 6,00 L/100 km auf 500 km, Reihenfolge und gespeicherte Werte.
    /// </summary>
    [Fact]
    public void CreateTwoEntries_ShowsConsumption()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Assert.False(Exists("Tankbook.Vehicle.Entries"));
            Tankbook.OpenEntries("Golf");
            WaitForAutomationId("TankbookEntries.Empty");
            Assert.Equal("Golf", WaitForAutomationId("TankbookEntries.SelectVehicle").Name);
            Press("TankbookEntries.SelectVehicle");
            Assert.Equal("Fahrzeug „Golf“ auswählen", Tankbook.ReadHint("TankbookEntries.Vehicle.Option.0"));
            Press("TankbookEntries.SelectVehicle");
            WaitUntil(() => !Exists("TankbookEntries.Vehicle.Option.0"), "Die Auswahl im Kopf wurde nicht zugeklappt.");

            Press("TankbookEntries.Add");
            Assert.Equal("Neuer Vorgang", WaitForAutomationId("TankbookEntries.Form.Title").Name);
            Assert.Equal("Kraftstoff", WaitForAutomationId("TankbookEntries.Form.KindText").Name);
            Assert.Equal("L", WaitForAutomationId("TankbookEntries.Form.QuantityUnit").Name);
            Assert.Equal("€/L", WaitForAutomationId("TankbookEntries.Form.UnitPriceUnit").Name);
            Assert.Equal("km", WaitForAutomationId("TankbookEntries.Form.OdometerUnit").Name);
            Tankbook.FillEntryForm("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");
            WaitUntil(() => FindByAutomationId("TankbookEntries.Form.ComputedHint")?.Name == "Ergibt 74,36 €", "Der Rechenhinweis „Ergibt 74,36 €“ fehlt.");
            Tankbook.SaveEntryForm();
            WaitForTexts("TankbookEntry.Item.Consumption", "Verbrauch 8,00 L/100 km (ab Startstand)");
            WaitForTexts("TankbookEntry.Item.Distance", "Strecke 500 km");
            WaitForTexts("TankbookEntry.Item.ConsumptionNote", "Ab Startstand berechnet: Angenommen wird, dass der Tank beim Start voll war. Teilbetankungen davor zählen mit.");

            Press("TankbookEntries.Add");
            Tankbook.FillEntryForm("Beta Tankstelle", "30", string.Empty, "55,77", "11000");
            WaitUntil(() => FindByAutomationId("TankbookEntries.Form.ComputedHint")?.Name == "Ergibt 1,859 €/L", "Der Rechenhinweis „Ergibt 1,859 €/L“ fehlt.");
            Tankbook.SaveEntryForm();

            WaitForTexts("TankbookEntry.Item.Station", "Beta Tankstelle", "Alpha Tankstelle");
            WaitForTexts("TankbookEntry.Item.Consumption", "Verbrauch 6,00 L/100 km", "Verbrauch 8,00 L/100 km (ab Startstand)");
            WaitForTexts("TankbookEntry.Item.Distance", "Strecke 500 km", "Strecke 500 km");
            Assert.Single(Tankbook.FindAllOrdered("TankbookEntry.Item.ConsumptionNote"));
            WaitForTexts("TankbookEntry.Item.Total", "55,77 €", "74,36 €");
            WaitForTexts("TankbookEntry.Item.UnitPrice", "1,859 €/L", "1,859 €/L");
            WaitForTexts("TankbookEntry.Item.Quantity", "Menge 30,00 L", "Menge 40,00 L");

            var stored = TankbookTestDatabase.ReadEntries(DatabasePath);
            Assert.Equal(2, stored.Count);
            Assert.Equal((40m, 1.859m, 74.36m, 10_500, "Fuel"), (stored[0].Quantity, stored[0].UnitPrice, stored[0].TotalAmount, stored[0].OdometerKm, stored[0].Kind));
            Assert.Equal((30m, 1.859m, 55.77m, 11_000, "Beta Tankstelle"), (stored[1].Quantity, stored[1].UnitPrice, stored[1].TotalAmount, stored[1].OdometerKm, stored[1].StationName));
        });
    }
}
