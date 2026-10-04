using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;

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
            var tab = FindTab(tabTitle);
            Assert.True(tab is not null, $"Tab '{tabTitle}' wurde nicht gefunden.");

            tab!.Click();

            var expectedAutomationId = TabAutomationIds[tabTitle];
            var pageHeadline = MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(expectedAutomationId));
            Assert.True(pageHeadline is not null, $"Seite für Tab '{tabTitle}' wurde nach dem Wechsel nicht gefunden (erwartete AutomationId '{expectedAutomationId}').");
        }

        var startTab = FindTab(TabTitles[0]);
        Assert.True(startTab is not null, $"Tab '{TabTitles[0]}' wurde für die Rückkehr zur Startseite nicht gefunden.");
        startTab!.Click();

        var startAutomationId = TabAutomationIds[TabTitles[0]];
        var startHeadline = MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(startAutomationId));
        Assert.True(startHeadline is not null, $"Die Startseite (Favoriten) wurde nach der Rückkehr nicht gefunden (erwartete AutomationId '{startAutomationId}').");
    }

    private AutomationElement? FindTab(string title)
    {
        return MainWindow.FindFirstDescendant(cf => cf.ByName(title).Or(cf.ByAutomationId(title)));
    }
}
