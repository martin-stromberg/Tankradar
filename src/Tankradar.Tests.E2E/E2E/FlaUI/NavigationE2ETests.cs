namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Rauchtest: Startet die Windows-App und prüft, dass alle vier Navigationsbereiche erreichbar sind.
/// </summary>
public class NavigationE2ETests : E2ETestBase
{
    private static readonly string[] TabTitles = ["Favoriten", "Karte", "Tankbuch", "Optionen"];

    private static readonly Dictionary<string, string> TabAutomationIds = new()
    {
        ["Favoriten"] = "FavoritesPage.Headline",
        ["Karte"] = "MapPage.Headline",
        ["Tankbuch"] = "TankbookPage.Headline",
        ["Optionen"] = "SettingsPage.Headline",
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
            NavigateToTab(tabTitle, TabAutomationIds[tabTitle]);
        }

        NavigateToTab(TabTitles[0], TabAutomationIds[TabTitles[0]]);
    }
}
