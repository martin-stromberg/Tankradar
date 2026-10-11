using System.Text.RegularExpressions;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Basis für E2E-Tests der Startseite (Favoritengruppen mit Tankstellenkacheln und Bereich „In der Nähe“); die Testdaten entstehen wie bei den Favoriten über Suche, Detailansicht und „Zu Favoriten hinzufügen“.
/// </summary>
public abstract class HomeE2ETestBase : FavoritesE2ETestBase
{
    /// <summary>
    /// Das Muster der Altersangabe einer Preiszeile („vor 3 Min.“).
    /// </summary>
    protected const string AgePattern = @"^vor \d+ Min\.$";

    /// <summary>
    /// Das Muster einer Entfernungsangabe („0,2 km“ oder „120 m“).
    /// </summary>
    protected const string DistancePattern = @"^\d+[,.]?\d* ?[km]+$";

    /// <summary>
    /// Wechselt auf die Startseite und wartet, bis die Gruppen mit den angegebenen Namen erscheinen; ohne Namen wird der Leerhinweis erwartet.
    /// </summary>
    /// <param name="expectedGroups">Die erwarteten Gruppennamen in Reihenfolge.</param>
    protected void OpenHome(params string[] expectedGroups)
    {
        if (expectedGroups.Length == 0)
        {
            NavigateToTab("Favoriten", "FavoritesPage.Headline");
            WaitForAutomationId("Favorites.Empty");
            return;
        }

        OpenFavorites(expectedGroups);
    }

    /// <summary>
    /// Legt aus der Detailansicht der Tankstelle (0 = Alpha, 1 = Beta) eine neue Gruppe an, wartet, bis die Details (Öffnungszeiten) geladen sind, und kehrt zur Ergebnisliste zurück.
    /// </summary>
    /// <param name="index">Der Listenplatz der Tankstelle.</param>
    /// <param name="stationName">Der Name der Tankstelle.</param>
    /// <param name="groupName">Der Name der neuen Gruppe.</param>
    protected void CreateGroupWithStation(int index, string stationName, string groupName)
    {
        OpenStationDetail(index, stationName);
        WaitForAutomationId("Detail.OpeningHours");
        AddToNewGroup(groupName, groupName);
        LeaveDetail();
    }

    /// <summary>
    /// Legt die Tankstelle (0 = Alpha, 1 = Beta) über die Detailansicht einer bestehenden Gruppe zu und kehrt zur Ergebnisliste zurück.
    /// </summary>
    /// <param name="index">Der Listenplatz der Tankstelle.</param>
    /// <param name="stationName">Der Name der Tankstelle.</param>
    /// <param name="groupName">Der Name der bestehenden Gruppe.</param>
    protected void AddStationToExistingGroup(int index, string stationName, string groupName)
    {
        OpenStationDetail(index, stationName);
        WaitForAutomationId("Detail.OpeningHours");
        Press("Detail.Favorites.Add");
        PressNamed("Detail.Favorites.Pick", groupName);
        WaitForTexts("Detail.Favorites.Group", groupName);
        LeaveDetail();
    }

    /// <summary>
    /// Legt zwei Gruppen an: „Arbeitsweg“ mit Alpha sowie „Heimat“ mit Alpha und Beta.
    /// </summary>
    protected void CreateTwoGroups()
    {
        CreateGroupWithStation(0, "Alpha Tankstelle", "Arbeitsweg");
        OpenStationDetail(0, "Alpha Tankstelle");
        AddToNewGroup("Heimat", "Arbeitsweg", "Heimat");
        LeaveDetail();
        AddStationToExistingGroup(1, "Beta Tankstelle", "Heimat");
    }

    /// <summary>
    /// Schaltet in den Optionen den Schalter „In der Nähe“ ein.
    /// </summary>
    protected void EnableNearby()
    {
        OpenSettings();
        var toggle = WaitForAutomationId("Settings.Nearby.Switch").Patterns.Toggle.Pattern;
        if (toggle.ToggleState.Value != ToggleState.On)
        {
            toggle.Toggle();
        }

        WaitUntil(
            () => WaitForAutomationId("Settings.Nearby.Switch").Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On,
            "Der Schalter 'In der Nähe' wurde nicht eingeschaltet.");
    }

    /// <summary>
    /// Liefert die Sortenbezeichnungen aller Kacheln in Seitenreihenfolge.
    /// </summary>
    /// <param name="prefix">Das Präfix der AutomationIds („Favorites.Station“ oder „Nearby.Station“).</param>
    /// <returns>Die Bezeichnungen („Super E5“, „Diesel“ usw.).</returns>
    protected IReadOnlyList<string> ReadFuelLabels(string prefix)
    {
        return MainWindow
            .FindAllDescendants(cf => new OrCondition(DefaultFuelTypeOrder.Select(fuel => (ConditionBase)cf.ByAutomationId($"{prefix}.Fuel.{fuel}")).ToArray()))
            .Select(element => element.Name)
            .ToList();
    }

    /// <summary>
    /// Wartet, bis der Abruf der Startseite beendet ist (Schaltfläche „Preise aktualisieren“ bedienbar) und mindestens ein Preisabruf am Mock ankam.
    /// </summary>
    protected void WaitForRefreshDone()
    {
        WaitUntil(
            () => Server.PricesRequests >= 1 && WaitForAutomationId("Favorites.Refresh").IsEnabled,
            "Der Preisabruf der Startseite wurde nicht abgeschlossen.");
    }

    /// <summary>
    /// Kehrt aus der Detailansicht mit dem Zurück-Pfeil der Kopfleiste zur Ergebnisliste der Karte zurück.
    /// </summary>
    protected void LeaveDetail()
    {
        WaitForAutomationId("NavigationViewBackButton").Patterns.Invoke.Pattern.Invoke();
        WaitForAutomationId("MapPage.Headline");
    }
}
