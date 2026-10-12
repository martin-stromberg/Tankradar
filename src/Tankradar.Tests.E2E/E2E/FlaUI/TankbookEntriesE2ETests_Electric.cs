namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der Ladevorgänge eines Elektrofahrzeugs: Einheiten kWh und €/kWh, frei eingegebener Ladeort, Verbrauch in kWh/100 km.
/// </summary>
public class TankbookEntriesE2ETests_Electric : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft zwei Ladevorgänge „Zuhause“: 33 kWh auf 200 km ergeben 16,50 kWh/100 km.
    /// </summary>
    [Fact]
    public void ChargingEntries_ShowKwhConsumption()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Zoe", "Electric", "5000", string.Empty);
            Tankbook.OpenEntries("Zoe");

            Press("TankbookEntries.Add");
            Assert.Equal("Strom", WaitForAutomationId("TankbookEntries.Form.KindText").Name);
            Assert.Equal("kWh", WaitForAutomationId("TankbookEntries.Form.QuantityUnit").Name);
            Assert.Equal("€/kWh", WaitForAutomationId("TankbookEntries.Form.UnitPriceUnit").Name);
            Assert.False(Exists("TankbookEntries.Form.Kind.Fuel"));
            Tankbook.FillEntryForm("Zuhause", "40", "0,59", string.Empty, "5200");
            Tankbook.SaveEntryForm();
            Tankbook.CreateEntry("Zuhause", "33", "0,59", string.Empty, "5400");

            WaitForTexts("TankbookEntry.Item.Consumption", "Verbrauch 16,50 kWh/100 km", "Noch kein Verbrauch: erster voller Vorgang");
            WaitForTexts("TankbookEntry.Item.UnitPrice", "0,59 €/kWh", "0,59 €/kWh");
            WaitForTexts("TankbookEntry.Item.Quantity", "Menge 33,00 kWh", "Menge 40,00 kWh");
            WaitForTexts("TankbookEntry.Item.Station", "Zuhause", "Zuhause");
            Assert.All(TankbookTestDatabase.ReadEntries(DatabasePath), entry => Assert.Equal("Electricity", entry.Kind));
        });
    }
}
