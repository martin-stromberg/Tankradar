namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test des Hinweises im Formular: Der Teilbetankungs-Schalter zeigt und verbirgt den präzisierten Hinweis.
/// </summary>
public class TankbookEntriesE2ETests_PartialHint : TankbookE2ETestBase
{
    private const string ExpectedHint = "Der Verbrauch wird erst ab dem ersten vollen Vorgang berechnet. Die Menge einer Teilbetankung fließt in den nächsten vollen Vorgang ein; "
        + "Teilbetankungen vor dem ersten vollen Vorgang zählen nicht für den Verbrauch, ihre Kosten erscheinen aber in den Monatskosten.";

    /// <summary>
    /// Prüft, dass der Hinweis nur bei eingeschaltetem Schalter erscheint und den vereinbarten Wortlaut hat.
    /// </summary>
    [Fact]
    public void PartialSwitch_ShowsAndHidesHint()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Press("TankbookEntries.Add");
            WaitForAutomationId("TankbookEntries.Form.Partial");
            Assert.False(Exists("TankbookEntries.Form.PartialHint"));

            Tankbook.ToggleSwitch("TankbookEntries.Form.Partial", on: true);

            Assert.Equal(ExpectedHint, WaitForAutomationId("TankbookEntries.Form.PartialHint").Name);

            Tankbook.ToggleSwitch("TankbookEntries.Form.Partial", on: false);

            WaitUntil(() => !Exists("TankbookEntries.Form.PartialHint"), "Der Hinweis zur Teilbetankung blieb stehen.");
        });
    }
}
