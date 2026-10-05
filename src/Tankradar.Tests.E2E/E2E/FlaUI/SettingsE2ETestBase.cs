using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Gemeinsame Hilfsmethoden für E2E-Tests der Optionen-Seite (Navigation, Warten, Bedienen von Schaltern, Auswahlfeldern und Schaltflächen).
/// </summary>
public abstract class SettingsE2ETestBase : E2ETestBase
{
    /// <summary>
    /// Alle Spritsorten in der Standardreihenfolge (Namen entsprechen den AutomationIds).
    /// </summary>
    protected static readonly string[] DefaultFuelTypeOrder = ["SuperE5", "SuperE10", "Diesel"];

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Startet die App mit Standardkonfiguration.
    /// </summary>
    protected SettingsE2ETestBase()
    {
    }

    /// <summary>
    /// Startet die App mit zusätzlichen Umgebungsvariablen (Testkonfiguration).
    /// </summary>
    /// <param name="additionalEnvironment">Die zusätzlichen Umgebungsvariablen.</param>
    protected SettingsE2ETestBase(IReadOnlyDictionary<string, string> additionalEnvironment)
        : base(additionalEnvironment)
    {
    }

    /// <summary>
    /// Wechselt auf die Optionen-Seite und wartet, bis die Einstellungen angezeigt und geladen sind.
    /// </summary>
    protected void OpenSettings()
    {
        NavigateToTab("Optionen", "SettingsPage.Headline");
        WaitForElement(cf => cf.ByAutomationId("Settings.FuelType.SuperE5.Switch"));
        WaitForAutomationId("Settings.Loaded");
    }

    /// <summary>
    /// Wartet, bis ein Element mit der AutomationId vorhanden ist.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns>Das gefundene Element.</returns>
    protected AutomationElement WaitForAutomationId(string automationId)
    {
        return WaitForElement(cf => cf.ByAutomationId(automationId));
    }

    /// <summary>
    /// Prüft, ob ein Element mit der AutomationId aktuell existiert.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns><see langword="true"/>, wenn das Element existiert.</returns>
    protected bool Exists(string automationId)
    {
        return MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId)) is not null;
    }

    /// <summary>
    /// Liefert, ob die Option mit der AutomationId ausgewählt ist.
    /// </summary>
    /// <param name="automationId">Die AutomationId des Auswahlfelds.</param>
    /// <returns><see langword="true"/>, wenn ausgewählt.</returns>
    protected bool IsRadioChecked(string automationId)
    {
        var element = MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        return element?.AsRadioButton().IsChecked == true;
    }

    /// <summary>
    /// Wählt die Option mit der AutomationId aus.
    /// </summary>
    /// <param name="automationId">Die AutomationId des Auswahlfelds.</param>
    protected void SelectRadio(string automationId)
    {
        WaitForAutomationId(automationId).AsRadioButton().IsChecked = true;
        WaitUntil(() => IsRadioChecked(automationId), $"Die Option '{automationId}' wurde nicht ausgewählt.");
    }

    /// <summary>
    /// Liefert, ob der Schalter einer Spritsorte eingeschaltet ist.
    /// </summary>
    /// <param name="fuelType">Der Spritsortenname (z. B. SuperE5).</param>
    /// <returns><see langword="true"/>, wenn die Sorte ausgewählt ist.</returns>
    protected bool IsFuelTypeSelected(string fuelType)
    {
        var element = WaitForAutomationId($"Settings.FuelType.{fuelType}.Switch");
        return element.Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On;
    }

    /// <summary>
    /// Schaltet den Schalter einer Spritsorte um.
    /// </summary>
    /// <param name="fuelType">Der Spritsortenname.</param>
    protected void ToggleFuelType(string fuelType)
    {
        WaitForAutomationId($"Settings.FuelType.{fuelType}.Switch").Patterns.Toggle.Pattern.Toggle();
    }

    /// <summary>
    /// Betätigt die Schaltfläche zum Verschieben einer Spritsorte.
    /// </summary>
    /// <param name="fuelType">Der Spritsortenname.</param>
    /// <param name="direction">Entweder <c>MoveUp</c> oder <c>MoveDown</c>.</param>
    protected void MoveFuelType(string fuelType, string direction)
    {
        WaitForAutomationId($"Settings.FuelType.{fuelType}.{direction}").Patterns.Invoke.Pattern.Invoke();
    }

    /// <summary>
    /// Liefert die Spritsorten in der Reihenfolge, in der sie auf der Seite von oben nach unten erscheinen.
    /// </summary>
    /// <returns>Die Spritsortennamen von oben nach unten.</returns>
    protected string[] ReadFuelTypeOrder()
    {
        return DefaultFuelTypeOrder
            .Select(fuelType => (fuelType, top: WaitForAutomationId($"Settings.FuelType.{fuelType}.Switch").BoundingRectangle.Top))
            .OrderBy(item => item.top)
            .Select(item => item.fuelType)
            .ToArray();
    }

    /// <summary>
    /// Wartet, bis die Bedingung erfüllt ist; schlägt andernfalls mit der Meldung fehl.
    /// </summary>
    /// <param name="condition">Die Bedingung.</param>
    /// <param name="failureMessage">Die Fehlermeldung bei Zeitüberschreitung.</param>
    protected static void WaitUntil(Func<bool> condition, string failureMessage)
    {
        var deadline = DateTime.UtcNow + DefaultTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            Thread.Sleep(100);
        }

        Assert.Fail(failureMessage);
    }

    private AutomationElement WaitForElement(Func<ConditionFactory, ConditionBase> condition)
    {
        AutomationElement? found = null;
        WaitUntil(() => (found = MainWindow.FindFirstDescendant(condition)) is not null, "Ein erwartetes Element wurde nicht gefunden.");
        return found!;
    }
}
