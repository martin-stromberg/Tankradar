namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Basis für E2E-Tests der Favoritengruppen: Die App läuft im Testmodus mit Mock-Preisdienst (Alpha und Beta Tankstelle), nie gegen produktive Dienste.
/// Die Hilfsmethoden bedienen ausschließlich UI-Automation-Muster (kein Mausklick), damit der Off-Screen-Betrieb funktioniert.
/// </summary>
public abstract class FavoritesE2ETestBase : SearchE2ETestBase
{
    /// <summary>
    /// Sucht und öffnet die Detailansicht der Tankstelle mit dem angegebenen Listenplatz (0 = Alpha, 1 = Beta) und wartet, bis die Karte „Favoritengruppen“ erscheint.
    /// </summary>
    /// <param name="index">Der Listenplatz in der Ergebnisliste.</param>
    /// <param name="expectedName">Der erwartete Name der Tankstelle.</param>
    protected void OpenStationDetail(int index, string expectedName)
    {
        OpenSearch();
        Submit();
        WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
        FindAllOrdered("Search.Station.Open")[index].Patterns.Invoke.Pattern.Invoke();
        WaitUntil(() => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("Detail.Name"))?.Name == expectedName, $"Die Detailansicht zeigt nicht '{expectedName}'.");
        WaitForAutomationId("Detail.Favorites.Add");
    }

    /// <summary>
    /// Löst die Schaltfläche mit der AutomationId aus.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    protected void Press(string automationId)
    {
        WaitForAutomationId(automationId).Patterns.Invoke.Pattern.Invoke();
    }

    /// <summary>
    /// Trägt den Text in das Eingabefeld mit der AutomationId ein und wartet, bis er dort steht.
    /// </summary>
    /// <param name="automationId">Die AutomationId des Eingabefelds.</param>
    /// <param name="text">Der Text.</param>
    protected void Type(string automationId, string text)
    {
        WaitForAutomationId(automationId).Patterns.Value.Pattern.SetValue(text);
        WaitUntil(
            () => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId))?.Patterns.Value.PatternOrDefault?.Value.ValueOrDefault == text,
            $"Das Feld '{automationId}' enthält nicht '{text}'.");
    }

    /// <summary>
    /// Wartet, bis die Texte aller Elemente mit der AutomationId genau den erwarteten Werten in dieser Reihenfolge entsprechen.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <param name="expected">Die erwarteten Texte.</param>
    protected void WaitForTexts(string automationId, params string[] expected)
    {
        var last = string.Empty;
        WaitUntil(
            () =>
            {
                var texts = ReadTexts(automationId);
                last = string.Join(" | ", texts);
                return texts.SequenceEqual(expected);
            },
            $"'{automationId}' zeigt nicht [{string.Join(" | ", expected)}], zuletzt: [{last}].");
    }

    /// <summary>
    /// Wartet, bis das Element mit der AutomationId den Text zeigt.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <param name="expected">Der erwartete Text.</param>
    protected void WaitForText(string automationId, string expected)
    {
        WaitForTexts(automationId, expected);
    }

    /// <summary>
    /// Legt in der Detailansicht über „Zu Favoriten hinzufügen“ eine neue Gruppe an und wartet, bis die Tankstelle ihr zugeordnet ist.
    /// </summary>
    /// <param name="name">Der Name der neuen Gruppe.</param>
    /// <param name="expectedGroups">Die Gruppen, die danach in der Karte angezeigt werden.</param>
    protected void AddToNewGroup(string name, params string[] expectedGroups)
    {
        Press("Detail.Favorites.Add");
        Type("Detail.Favorites.NewName", name);
        Press("Detail.Favorites.NewCreate");
        WaitForTexts("Detail.Favorites.Group", expectedGroups);
        WaitUntil(() => !Exists("Detail.Favorites.NewName"), "Die Auswahl der Gruppen wurde nicht geschlossen.");
    }

    /// <summary>
    /// Wechselt in den Bereich „Favoriten“ und wartet, bis die Gruppenliste die angegebenen Gruppen zeigt.
    /// </summary>
    /// <param name="expectedGroups">Die erwarteten Gruppennamen in Reihenfolge.</param>
    protected void OpenFavorites(params string[] expectedGroups)
    {
        NavigateToTab("Favoriten", "FavoritesPage.Headline");
        WaitForTexts("Favorites.Group.Name", expectedGroups);
    }

    /// <summary>
    /// Öffnet die Gruppenansicht der Gruppe mit dem angegebenen Namen aus der Gruppenliste.
    /// </summary>
    /// <param name="name">Der Name der Gruppe.</param>
    protected void OpenGroup(string name)
    {
        var index = ReadTexts("Favorites.Group.Name").ToList().IndexOf(name);
        Assert.True(index >= 0, $"Die Gruppe '{name}' steht nicht in der Liste.");
        FindAllOrdered("Favorites.Group.Open")[index].Patterns.Invoke.Pattern.Invoke();
        WaitForText("FavoriteGroup.Name", name);
    }

    /// <summary>
    /// Bestätigt, dass das Element mit der AutomationId als ausgewählt gilt (Hinweistext „Ausgewählt“), wobei mehrere Elemente dieselbe AutomationId tragen können.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns>Die Namen der ausgewählten Elemente.</returns>
    protected IReadOnlyList<string> SelectedNames(string automationId)
    {
        return FindAllOrdered(automationId)
            .Where(element => element.Properties.HelpText.ValueOrDefault == "Ausgewählt")
            .Select(element => element.Name)
            .ToList();
    }

    /// <summary>
    /// Wählt in einer Gruppe gleichnamiger Schaltflächen (z. B. Auswahl der Gruppen) die mit dem Namen aus.
    /// </summary>
    /// <param name="automationId">Die AutomationId der Schaltflächen.</param>
    /// <param name="name">Der Name der gesuchten Schaltfläche.</param>
    protected void PressNamed(string automationId, string name)
    {
        var element = FindAllOrdered(automationId).FirstOrDefault(candidate => candidate.Name == name);
        Assert.True(element is not null, $"Keine Schaltfläche '{name}' mit der AutomationId '{automationId}'.");
        element.Patterns.Invoke.Pattern.Invoke();
    }
}
