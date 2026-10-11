using FlaUI.Core.AutomationElements;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Basis für E2E-Tests des Tankbuchs: Die App läuft im Testmodus ohne Mock-Server (das Tankbuch braucht keinen Dienst; Preis- und Geokodierungsabrufe sind ohne
/// Adresse verweigert). Die Hilfsmethoden bedienen ausschließlich UI-Automation-Muster (kein Mausklick), damit der Off-Screen-Betrieb funktioniert.
/// </summary>
public abstract class TankbookE2ETestBase : SettingsE2ETestBase
{
    /// <summary>
    /// Der Pfad der Datenbankdatei der laufenden App.
    /// </summary>
    protected string DatabasePath
    {
        get { return Path.Combine(TestDataDirectory, "tankatlas.db"); }
    }

    /// <summary>
    /// Wechselt in den Bereich „Tankbuch“ und wartet, bis „Fahrzeug hinzufügen“ angezeigt wird.
    /// </summary>
    protected void OpenTankbook()
    {
        NavigateToTab("Tankbuch", "TankbookPage.Headline");
        WaitForAutomationId("Tankbook.AddVehicle");
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
            () => FindByAutomationId(automationId)?.Patterns.Value.PatternOrDefault?.Value.ValueOrDefault == text,
            $"Das Feld '{automationId}' enthält nicht '{text}'.");
    }

    /// <summary>
    /// Liefert die Texte aller Elemente mit der AutomationId von oben nach unten; ein Element, das gerade neu aufgebaut wird, führt zu einer leeren Liste.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns>Die Texte.</returns>
    protected IReadOnlyList<string> ReadTexts(string automationId)
    {
        try
        {
            return FindAllOrdered(automationId).Select(element => element.Name).ToList();
        }
        catch (global::FlaUI.Core.Exceptions.PropertyNotSupportedException)
        {
            return [];
        }
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
    /// Löst die Schaltfläche mit der AutomationId im Eintrag des Fahrzeugs mit dem Namen aus (gleicher Listenplatz wie der Name).
    /// </summary>
    /// <param name="automationId">Die AutomationId der Schaltfläche im Eintrag.</param>
    /// <param name="vehicleName">Der Name des Fahrzeugs.</param>
    protected void PressForVehicle(string automationId, string vehicleName)
    {
        WaitUntil(() => ReadTexts("Tankbook.Vehicle.Name").Contains(vehicleName), $"Das Fahrzeug '{vehicleName}' steht nicht in der Liste.");
        var index = ReadTexts("Tankbook.Vehicle.Name").ToList().IndexOf(vehicleName);
        AutomationElement? button = null;
        WaitUntil(
            () =>
            {
                var buttons = FindAllOrdered(automationId);
                button = buttons.Count > index ? buttons[index] : null;
                return button is not null;
            },
            $"Die Schaltfläche '{automationId}' des Fahrzeugs '{vehicleName}' fehlt.");
        button!.Patterns.Invoke.Pattern.Invoke();
    }

    /// <summary>
    /// Füllt das geöffnete Formular aus.
    /// </summary>
    /// <param name="name">Der Name.</param>
    /// <param name="fuelKindKey">Der Schlüssel der Kraftstoffart (Petrol, Diesel, Electric, Hybrid).</param>
    /// <param name="odometer">Der Start-Kilometerstand.</param>
    /// <param name="consumption">Der Startverbrauch (leer für keinen).</param>
    protected void FillVehicleForm(string name, string fuelKindKey, string odometer, string consumption)
    {
        Type("Tankbook.Form.Name", name);
        Press($"Tankbook.Form.FuelKind.{fuelKindKey}");
        WaitUntil(
            () => FindByAutomationId($"Tankbook.Form.FuelKind.{fuelKindKey}")?.Properties.HelpText.ValueOrDefault == "Ausgewählt",
            $"Die Kraftstoffart '{fuelKindKey}' wurde nicht ausgewählt.");
        Type("Tankbook.Form.Odometer", odometer);
        Type("Tankbook.Form.Consumption", consumption);
    }

    /// <summary>
    /// Betätigt „Speichern“ und wartet, bis das Formular geschlossen ist.
    /// </summary>
    protected void SaveVehicleForm()
    {
        Press("Tankbook.Form.Save");
        WaitUntil(() => !Exists("Tankbook.Form.Save"), "Das Formular wurde nach dem Speichern nicht geschlossen.");
    }

    /// <summary>
    /// Legt ein Fahrzeug über „Fahrzeug hinzufügen“ an.
    /// </summary>
    /// <param name="name">Der Name.</param>
    /// <param name="fuelKindKey">Der Schlüssel der Kraftstoffart.</param>
    /// <param name="odometer">Der Start-Kilometerstand.</param>
    /// <param name="consumption">Der Startverbrauch (leer für keinen).</param>
    protected void CreateVehicle(string name, string fuelKindKey, string odometer, string consumption)
    {
        Press("Tankbook.AddVehicle");
        WaitForAutomationId("Tankbook.Form.Name");
        FillVehicleForm(name, fuelKindKey, odometer, consumption);
        SaveVehicleForm();
        WaitUntil(() => ReadTexts("Tankbook.Vehicle.Name").Contains(name), $"Das Fahrzeug '{name}' erscheint nicht in der Liste.");
    }

    /// <summary>
    /// Liefert die Elemente mit der AutomationId in Dokumentreihenfolge, die bei der senkrechten Liste der Reihenfolge von oben nach unten entspricht.
    /// Elemente außerhalb des sichtbaren Bereichs melden keine Position, daher wird nicht nach Koordinaten sortiert.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns>Die Elemente.</returns>
    protected IReadOnlyList<AutomationElement> FindAllOrdered(string automationId)
    {
        return MainWindow.FindAllDescendants(cf => cf.ByAutomationId(automationId));
    }
}
