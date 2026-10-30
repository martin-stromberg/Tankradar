using FlaUI.Core.Definitions;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Rauchtest: Startet die Windows-App und prüft, dass alle vier Navigationsbereiche erreichbar sind.
/// </summary>
public class NavigationE2ETests : E2ETestBase
{
    private static readonly string[] TabTitles = ["Favoriten", "Karte", "Tankbuch", "Optionen"];

    // Je Reiter ein Element im Seiteninhalt, das erst nach dem Aufbau der Seite existiert.
    private static readonly Dictionary<string, string> ContentIds = new()
    {
        ["Favoriten"] = "Favorites.Refresh",
        ["Karte"] = "Search.Overlay.Open",
        ["Tankbuch"] = "TankbookEntries.ManageVehicles",
        ["Optionen"] = "Settings.Loaded",
    };

    /// <summary>
    /// Prüft, dass die App startet, die AppShell mit vier Tabs anzeigt, durch alle Bereiche navigiert und wieder zur Startseite zurückkehrt.
    /// </summary>
    [Fact]
    public void AppStartsAndNavigatesThroughAllTabs()
    {
        RunWithDiagnostics(NavigateThroughAllTabs);
    }

    /// <summary>
    /// Prüft, dass das Hauptfenster den Anzeigenamen „Tankatlas“ als Titel trägt.
    /// </summary>
    [Fact]
    public void MainWindowShowsDisplayNameAsTitle()
    {
        RunWithDiagnostics(() => Assert.Equal("Tankatlas", MainWindow.Title));
    }

    private void NavigateThroughAllTabs()
    {
        Assert.NotNull(MainWindow);

        foreach (var tabTitle in TabTitles)
        {
            // Der Seitentitel steht nur in der Kopfzeile der Shell, nicht noch einmal als Überschrift im Seiteninhalt.
            NavigateToTab(tabTitle, ContentIds[tabTitle]);
            WaitForPageTitle(tabTitle);
            Assert.Single(MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Text).And(cf.ByName(tabTitle))));
        }

        NavigateToTab(TabTitles[0], ContentIds[TabTitles[0]]);
    }
}
