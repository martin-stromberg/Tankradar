namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Die Preiszeilen der Startseite folgen der Sortenreihenfolge der Optionen; abgewählte Sorten fehlen.
/// </summary>
public class HomeE2ETests_FuelOrder : HomeE2ETestBase
{
    /// <summary>
    /// Prüft die Standardreihenfolge und danach eine geänderte Reihenfolge mit abgewählter Sorte.
    /// </summary>
    [Fact]
    public void FuelOrderAndSelection_AreReflectedOnHome()
    {
        RunWithDiagnostics(() =>
        {
            CreateGroupWithStation(0, "Alpha Tankstelle", "Arbeitsweg");
            OpenHome("Arbeitsweg");
            WaitUntil(() => ReadFuelLabels("Favorites.Station").SequenceEqual(["Super E5", "Super E10", "Diesel"]), "Die Standardreihenfolge der Sorten fehlt.");

            OpenSettings();
            MoveFuelType("Diesel", "MoveUp");
            MoveFuelType("Diesel", "MoveUp");
            ToggleFuelType("SuperE10");
            WaitUntil(() => !IsFuelTypeSelected("SuperE10"), "Super E10 wurde nicht abgewählt.");

            OpenHome("Arbeitsweg");

            WaitUntil(() => ReadFuelLabels("Favorites.Station").SequenceEqual(["Diesel", "Super E5"]), "Die geänderte Sortenreihenfolge fehlt.");
            Assert.False(Exists("Favorites.Station.Price.SuperE10"));
        });
    }
}
