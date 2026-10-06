namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Tests der Gruppenverwaltung: Gruppe anlegen, Löschen nach Rückfrage, Mehrfachzuordnung mit Auswahl beim Entfernen, Notiz und Priorität sowie Persistenz über einen Neustart.
/// </summary>
public class FavoritesE2ETests_Groups : FavoritesE2ETestBase
{
    /// <summary>
    /// Prüft, dass eine Gruppe mit Beschreibung angelegt, ohne Rückfrage nicht gelöscht und nach Bestätigung gelöscht wird.
    /// </summary>
    [Fact]
    public void CreateGroupWithDescription_DeleteAsksForConfirmation()
    {
        RunWithDiagnostics(() =>
        {
            NavigateToTab("Favoriten", "FavoritesPage.Headline");
            WaitForAutomationId("Favorites.Empty");

            Press("Favorites.NewGroup");
            Type("Favorites.NewName", "Urlaub");
            Type("Favorites.NewDescription", "Tankstellen an der Küste");
            Press("Favorites.NewCreate");
            WaitForTexts("Favorites.Group.Name", "Urlaub");
            WaitForText("Favorites.Group.Description", "Tankstellen an der Küste");
            WaitForText("Favorites.Group.Count", "0 Tankstellen");

            OpenGroup("Urlaub");
            WaitForAutomationId("FavoriteGroup.Empty");
            Press("FavoriteGroup.Delete");
            Assert.Contains("„Urlaub“", WaitForAutomationId("FavoriteGroup.DeleteQuestion").Name, StringComparison.Ordinal);
            Press("FavoriteGroup.DeleteCancel");
            WaitUntil(() => !Exists("FavoriteGroup.DeleteQuestion"), "Die Rückfrage blieb stehen.");
            Assert.Equal("Urlaub", WaitForAutomationId("FavoriteGroup.Name").Name);

            Press("FavoriteGroup.Delete");
            Press("FavoriteGroup.DeleteConfirm");
            WaitForAutomationId("Favorites.Empty");
            Assert.False(Exists("Favorites.Group.Name"));
        });
    }

    /// <summary>
    /// Prüft, dass ein doppelter Gruppenname gemeldet wird.
    /// </summary>
    [Fact]
    public void CreateGroup_DuplicateName_ShowsMessage()
    {
        RunWithDiagnostics(() =>
        {
            NavigateToTab("Favoriten", "FavoritesPage.Headline");
            Press("Favorites.NewGroup");
            Type("Favorites.NewName", "Heimat");
            Press("Favorites.NewCreate");
            WaitForTexts("Favorites.Group.Name", "Heimat");

            Press("Favorites.NewGroup");
            Type("Favorites.NewName", "heimat");
            Press("Favorites.NewCreate");

            WaitForText("Favorites.Status", "Eine Gruppe mit diesem Namen gibt es bereits.");
            WaitForTexts("Favorites.Group.Name", "Heimat");
        });
    }

    /// <summary>
    /// Prüft, dass eine Tankstelle mehreren Gruppen angehören kann (neue und bestehende Gruppe) und beim Entfernen gewählt wird, aus welcher Gruppe sie entfernt wird.
    /// </summary>
    [Fact]
    public void MultipleGroups_RemoveAsksWhichGroup()
    {
        RunWithDiagnostics(() =>
        {
            OpenStationDetail(0, "Alpha Tankstelle");
            AddToNewGroup("Arbeitsweg", "Arbeitsweg");
            AddToNewGroup("Heimat", "Arbeitsweg", "Heimat");

            Press("Detail.Favorites.Add");
            WaitForAutomationId("Detail.Favorites.AllAssigned");
            Press("Detail.Favorites.Cancel");

            Press("Detail.Favorites.Remove");
            WaitForTexts("Detail.Favorites.RemoveChoice", "Arbeitsweg", "Heimat");
            Assert.False(WaitForAutomationId("Detail.Favorites.RemoveConfirm").IsEnabled);
            PressNamed("Detail.Favorites.RemoveChoice", "Arbeitsweg");
            WaitUntil(() => SelectedNames("Detail.Favorites.RemoveChoice").SequenceEqual(["Arbeitsweg"]), "Die Gruppe wurde nicht ausgewählt.");
            Press("Detail.Favorites.RemoveConfirm");

            WaitForTexts("Detail.Favorites.Group", "Heimat");
            WaitUntil(() => !Exists("Detail.Favorites.RemoveChoice"), "Die Auswahl zum Entfernen blieb offen.");

            // Die entfernte Gruppe steht wieder zur Wahl; die Zuordnung zu einer bestehenden Gruppe geschieht über die Auswahl.
            Press("Detail.Favorites.Add");
            WaitForTexts("Detail.Favorites.Pick", "Arbeitsweg");
            PressNamed("Detail.Favorites.Pick", "Arbeitsweg");
            WaitForTexts("Detail.Favorites.Group", "Arbeitsweg", "Heimat");
        });
    }

    /// <summary>
    /// Prüft, dass Notiz und Priorität eines Eintrags gepflegt und angezeigt werden.
    /// </summary>
    [Fact]
    public void EntryNoteAndPriority_AreSavedAndShown()
    {
        RunWithDiagnostics(() =>
        {
            OpenStationDetail(0, "Alpha Tankstelle");
            AddToNewGroup("Arbeitsweg", "Arbeitsweg");
            OpenFavorites("Arbeitsweg");
            OpenGroup("Arbeitsweg");
            WaitForText("FavoriteGroup.Entry.Priority", "Priorität: Keine");

            Press("FavoriteGroup.Entry.Edit");
            Type("FavoriteGroup.Entry.NoteInput", "Immer günstig dienstags");
            Press("FavoriteGroup.Entry.Priority.High");
            WaitUntil(() => SelectedNames("FavoriteGroup.Entry.Priority.High").Count == 1, "Die Priorität wurde nicht ausgewählt.");
            Press("FavoriteGroup.Entry.Save");

            WaitForText("FavoriteGroup.Entry.Note", "Immer günstig dienstags");
            WaitForText("FavoriteGroup.Entry.Priority", "Priorität: Hoch");
            WaitUntil(() => !Exists("FavoriteGroup.Entry.NoteInput"), "Das Formular blieb offen.");
        });
    }

    /// <summary>
    /// Prüft, dass Gruppen und Zuordnungen einen Neustart der App überstehen (lokale Speicherung).
    /// </summary>
    [Fact]
    public void Favorites_SurviveApplicationRestart()
    {
        RunWithDiagnostics(() =>
        {
            OpenStationDetail(1, "Beta Tankstelle");
            AddToNewGroup("Urlaub", "Urlaub");

            RestartApplication();

            OpenFavorites("Urlaub");
            WaitForText("Favorites.Group.Count", "1 Tankstelle");
            OpenGroup("Urlaub");
            WaitForText("FavoriteGroup.Entry.Name", "Beta Tankstelle");
        });
    }
}
