namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test (Issue #20): Fahrzeugwechsel über die Auswahl im Kopf des Tabs „Tankbuch“; die Vorgänge bleiben je Fahrzeug getrennt.
/// </summary>
public class TankbookEntriesE2ETests_VehicleSwitch : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft, dass die Auswahl die Fahrzeuge nach Namen anbietet, der Wechsel auf „Polo“ keine Vorgänge von „Golf“ zeigt und der Wechsel zurück sie wieder zeigt.
    /// </summary>
    [Fact]
    public void SwitchVehicle_ShowsOnlyEntriesOfSelectedVehicle()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Polo", "Petrol", "5000", string.Empty);
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Tankbook.CreateEntry("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");

            Press("TankbookEntries.SelectVehicle");
            WaitUntil(() => Tankbook.ReadVehicleOptions().SequenceEqual(["Golf", "Polo"]), "Die Auswahl im Kopf bietet nicht „Golf“ und „Polo“ an.");
            Assert.Equal("Fahrzeug „Polo“ auswählen", Tankbook.ReadHint("TankbookEntries.Vehicle.Option.1"));

            Tankbook.SelectVehicle("Polo");
            WaitForAutomationId("TankbookEntries.Empty");
            Assert.Empty(ReadTexts("TankbookEntry.Item.Station"));
            WaitUntil(() => !Exists("TankbookEntries.Vehicle.Option.0"), "Die Auswahl im Kopf wurde nach dem Wechsel nicht zugeklappt.");
            Assert.Equal("Polo", WaitForAutomationId("TankbookEntries.SelectVehicle").Name);

            Tankbook.SelectVehicle("Golf");
            WaitForTexts("TankbookEntry.Item.Station", "Alpha Tankstelle");
        });
    }
}
