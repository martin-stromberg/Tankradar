namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test des Hauptablaufs der Favoritengruppen: Tankstelle suchen, Details öffnen, zu neuer Gruppe hinzufügen, Gruppe umbenennen, Tankstelle entfernen.
/// </summary>
public class FavoritesE2ETests_Flow : FavoritesE2ETestBase
{
    /// <summary>
    /// Prüft den gesamten Ablauf über Suche, Detailansicht, Gruppenübersicht und Gruppenansicht.
    /// </summary>
    [Fact]
    public void SearchOpenDetailAddToNewGroupRenameAndRemove()
    {
        RunWithDiagnostics(() =>
        {
            OpenStationDetail(0, "Alpha Tankstelle");
            Assert.True(Exists("Detail.Favorites.None"));
            Assert.False(Exists("Detail.Favorites.Remove"));

            AddToNewGroup("Arbeitsweg", "Arbeitsweg");
            Assert.True(Exists("Detail.Favorites.Remove"));
            Assert.False(Exists("Detail.Favorites.None"));

            OpenFavorites("Arbeitsweg");
            WaitForText("Favorites.Group.Count", "1 Tankstelle");
            OpenGroup("Arbeitsweg");
            WaitForText("FavoriteGroup.Entry.Name", "Alpha Tankstelle");
            WaitForText("FavoriteGroup.Entry.Address", "Hauptstraße 1, 10115 Berlin");

            Press("FavoriteGroup.Edit");
            Type("FavoriteGroup.EditName", "Pendeln");
            Type("FavoriteGroup.EditDescription", "Täglicher Weg");
            Press("FavoriteGroup.EditSave");
            WaitForText("FavoriteGroup.Name", "Pendeln");
            WaitForText("FavoriteGroup.Description", "Täglicher Weg");

            // Zurück zur Gruppenübersicht (Pfeil der Kopfleiste) und zur Detailansicht: Die Karte zeigt den neuen Gruppennamen, danach wird die Tankstelle entfernt.
            WaitForAutomationId("NavigationViewBackButton").Patterns.Invoke.Pattern.Invoke();
            WaitForTexts("Favorites.Group.Name", "Pendeln");
            NavigateToTab("Karte", "Detail.Favorites.Add");
            WaitForTexts("Detail.Favorites.Group", "Pendeln");
            Press("Detail.Favorites.Remove");
            WaitUntil(() => !Exists("Detail.Favorites.Group") && !Exists("Detail.Favorites.Remove"), "Die Tankstelle wurde nicht aus der Gruppe entfernt.");
            Assert.True(Exists("Detail.Favorites.None"));

            OpenFavorites("Pendeln");
            WaitForText("Favorites.Group.Count", "0 Tankstellen");
        });
    }
}
