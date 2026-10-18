using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test des Anzeigenamens von Tankstellen: Eine Tankstelle, deren Name der Inhaber ist, erscheint unter ihrer Marke mit dem Stammdatennamen klein darunter
/// (Suche, Detailansicht, Favoriten, Gruppenansicht, Tankbuch); ein in der Detailansicht vergebener eigener Name hat überall Vorrang, bleibt nach einem Neustart
/// erhalten und lässt sich wieder entfernen. Stationen ohne Marke behalten ihren Namen.
/// </summary>
public class FavoritesE2ETests_DisplayName : HomeE2ETestBase
{
    private const string OwnerName = "Klaus Heinrich";
    private const string Brand = "Westfalen";
    private const string CustomName = "Zuhause tanken";

    /// <summary>
    /// Erstellt den Test mit einem Treiber für das Tankbuch.
    /// </summary>
    public FavoritesE2ETests_DisplayName()
    {
        Tankbook = new TankbookE2EDriver(() => MainWindow, SetFieldValue, WaitUntil, NavigateToTab);
        Server.AddOwnerNamedStation();
    }

    private TankbookE2EDriver Tankbook { get; }

    private void OpenListedStationDetail(int index, string expectedName)
    {
        OpenSearch();
        Submit();
        WaitForStationNames(Brand, "Alpha Tankstelle", "Beta Tankstelle");
        WaitForTexts("Search.Station.SubName", OwnerName);
        FindAllOrdered("Search.Station.Open")[index].Patterns.Invoke.Pattern.Invoke();
        WaitUntil(() => FindByAutomationId("Detail.Name")?.Name == expectedName, $"Die Detailansicht zeigt nicht '{expectedName}'.");
        WaitForAutomationId("Detail.Favorites.Add");
    }

    private void OpenOwnerStationDetail(string expectedName)
    {
        OpenListedStationDetail(0, expectedName);
    }

    /// <summary>
    /// Prüft die Marke vorn mit Stammdatennamen darunter in Suche, Detailansicht, Favoriten und Gruppenansicht; eine Tankstelle ohne Marke zeigt nur ihren Namen.
    /// </summary>
    [Fact]
    public void OwnerNamedStation_ShowsBrandWithSubNameEverywhere()
    {
        RunWithDiagnostics(() =>
        {
            OpenOwnerStationDetail(Brand);
            WaitForText("Detail.SubName", OwnerName);
            AddToNewGroup("Heimat", "Heimat");

            OpenFavorites("Heimat");
            OpenGroup("Heimat");
            WaitForText("FavoriteGroup.Entry.Name", Brand);
            WaitForText("FavoriteGroup.Entry.SubName", OwnerName);
            WaitForAutomationId("NavigationViewBackButton").Patterns.Invoke.Pattern.Invoke();
            WaitForTexts("Favorites.Group.Name", "Heimat");

            NavigateToTab("Karte", "Detail.Favorites.Add");
            LeaveDetail();
            OpenListedStationDetail(1, "Alpha Tankstelle");
            Assert.False(Exists("Detail.SubName"), "Eine Tankstelle ohne Marke darf keinen zweiten Namen zeigen.");
            AddToNewGroup("Arbeitsweg", "Arbeitsweg");

            OpenFavorites("Arbeitsweg", "Heimat");
            WaitForTexts("Favorites.Station.Name", "Alpha Tankstelle", Brand);
            WaitForTexts("Favorites.Station.SubName", OwnerName);
        });
    }

    /// <summary>
    /// Prüft Vergeben, Anzeige in Detailansicht, Favoriten, Gruppenansicht und Tankbuch-Auswahl, Neustart und Entfernen des eigenen Namens.
    /// </summary>
    [Fact]
    public void CustomName_SetShownEverywhereSurvivesRestartAndCanBeRemoved()
    {
        RunWithDiagnostics(() =>
        {
            OpenOwnerStationDetail(Brand);
            AddToNewGroup("Heimat", "Heimat");
            Assert.False(Exists("Detail.CustomName.Remove"), "Ohne eigenen Namen darf es nichts zu entfernen geben.");

            Type("Detail.CustomName.Input", CustomName);
            Press("Detail.CustomName.Save");
            WaitForText("Detail.CustomName.Message", "Der eigene Name wurde gespeichert.");
            WaitUntil(() => FindByAutomationId("Detail.Name")?.Name == CustomName, "Die Detailansicht zeigt den eigenen Namen nicht.");
            WaitForText("Detail.SubName", OwnerName);
            WaitForAutomationId("Detail.CustomName.Remove");

            OpenFavorites("Heimat");
            WaitForText("Favorites.Station.Name", CustomName);
            WaitForText("Favorites.Station.SubName", OwnerName);
            OpenGroup("Heimat");
            WaitForText("FavoriteGroup.Entry.Name", CustomName);
            WaitForText("FavoriteGroup.Entry.SubName", OwnerName);

            Tankbook.OpenTankbook();
            Tankbook.CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Tankbook.Press("TankbookEntries.Add");
            Assert.Equal(CustomName, Tankbook.WaitFor("TankbookEntries.Form.KnownStation.0").Name);

            RestartApplication(CreateEnvironment(Server.BaseUrl, TestLocation, MockTankerkoenigServer.AcceptedKey));
            OpenHome("Heimat");
            WaitForText("Favorites.Station.Name", CustomName);
            WaitForText("Favorites.Station.SubName", OwnerName);

            Press("Favorites.Station.Open");
            WaitUntil(() => FindByAutomationId("Detail.Name")?.Name == CustomName, "Die Detailansicht zeigt nach dem Neustart nicht den eigenen Namen.");
            Assert.Equal(CustomName, WaitForAutomationId("Detail.CustomName.Input").Patterns.Value.Pattern.Value.Value);
            Press("Detail.CustomName.Remove");
            WaitForText("Detail.CustomName.Message", "Der eigene Name wurde entfernt.");
            WaitUntil(() => FindByAutomationId("Detail.Name")?.Name == Brand, "Nach dem Entfernen zeigt die Detailansicht nicht wieder die Marke.");
            WaitUntil(() => !Exists("Detail.CustomName.Remove"), "Die Schaltfläche zum Entfernen blieb stehen.");

            WaitForAutomationId("NavigationViewBackButton").Patterns.Invoke.Pattern.Invoke();
            WaitForAutomationId("FavoritesPage.Headline");
            WaitForText("Favorites.Station.Name", Brand);
        });
    }
}
