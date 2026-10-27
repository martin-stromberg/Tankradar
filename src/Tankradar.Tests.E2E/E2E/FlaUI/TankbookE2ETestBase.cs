using FlaUI.Core.AutomationElements;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Basis für E2E-Tests des Tankbuchs: Die App läuft im Testmodus ohne Mock-Server (das Tankbuch braucht keinen Dienst; Preis- und Geokodierungsabrufe sind ohne
/// Adresse verweigert). Die Bedienung übernimmt <see cref="TankbookE2EDriver"/>; die Hilfsmethoden hier delegieren an ihn.
/// </summary>
public abstract class TankbookE2ETestBase : SettingsE2ETestBase
{
    /// <summary>
    /// Startet die App ohne zusätzliche Umgebungsvariablen.
    /// </summary>
    protected TankbookE2ETestBase()
    {
        Tankbook = CreateDriver();
    }

    /// <summary>
    /// Startet die App mit zusätzlichen Umgebungsvariablen (z. B. der Datei der Test-Fotoauswahl).
    /// </summary>
    /// <param name="additionalEnvironment">Die zusätzlichen Umgebungsvariablen.</param>
    protected TankbookE2ETestBase(IReadOnlyDictionary<string, string> additionalEnvironment)
        : base(additionalEnvironment)
    {
        Tankbook = CreateDriver();
    }

    /// <summary>
    /// Der Treiber für Fahrzeuge sowie Tank- und Ladevorgänge.
    /// </summary>
    protected TankbookE2EDriver Tankbook { get; }

    /// <summary>
    /// Wechselt in den Bereich „Tankbuch“ und wartet, bis „Fahrzeug hinzufügen“ angezeigt wird.
    /// </summary>
    protected void OpenTankbook()
    {
        Tankbook.OpenTankbook();
    }

    /// <summary>
    /// Löst die Schaltfläche mit der AutomationId aus.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    protected void Press(string automationId)
    {
        Tankbook.Press(automationId);
    }

    /// <summary>
    /// Trägt den Text in das Eingabefeld mit der AutomationId ein und wartet, bis er dort steht.
    /// </summary>
    /// <param name="automationId">Die AutomationId des Eingabefelds.</param>
    /// <param name="text">Der Text.</param>
    protected void Type(string automationId, string text)
    {
        Tankbook.Type(automationId, text);
    }

    /// <summary>
    /// Liefert die Texte aller Elemente mit der AutomationId von oben nach unten.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns>Die Texte.</returns>
    protected IReadOnlyList<string> ReadTexts(string automationId)
    {
        return Tankbook.ReadTexts(automationId);
    }

    /// <summary>
    /// Wartet, bis die Texte aller Elemente mit der AutomationId genau den erwarteten Werten in dieser Reihenfolge entsprechen.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <param name="expected">Die erwarteten Texte.</param>
    protected void WaitForTexts(string automationId, params string[] expected)
    {
        Tankbook.WaitForTexts(automationId, expected);
    }

    /// <summary>
    /// Löst die Schaltfläche mit der AutomationId im Eintrag des Fahrzeugs mit dem Namen aus.
    /// </summary>
    /// <param name="automationId">Die AutomationId der Schaltfläche im Eintrag.</param>
    /// <param name="vehicleName">Der Name des Fahrzeugs.</param>
    protected void PressForVehicle(string automationId, string vehicleName)
    {
        Tankbook.PressForVehicle(automationId, vehicleName);
    }

    /// <summary>
    /// Füllt das geöffnete Fahrzeugformular aus.
    /// </summary>
    /// <param name="name">Der Name.</param>
    /// <param name="fuelKindKey">Der Schlüssel der Kraftstoffart (Petrol, Diesel, Electric, Hybrid).</param>
    /// <param name="odometer">Der Start-Kilometerstand.</param>
    /// <param name="consumption">Der Startverbrauch (leer für keinen).</param>
    protected void FillVehicleForm(string name, string fuelKindKey, string odometer, string consumption)
    {
        Tankbook.FillVehicleForm(name, fuelKindKey, odometer, consumption);
    }

    /// <summary>
    /// Betätigt „Speichern“ und wartet, bis das Formular geschlossen ist.
    /// </summary>
    protected void SaveVehicleForm()
    {
        Tankbook.SaveVehicleForm();
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
        Tankbook.CreateVehicle(name, fuelKindKey, odometer, consumption);
    }

    /// <summary>
    /// Liefert die Elemente mit der AutomationId in Dokumentreihenfolge.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns>Die Elemente.</returns>
    protected IReadOnlyList<AutomationElement> FindAllOrdered(string automationId)
    {
        return Tankbook.FindAllOrdered(automationId);
    }

    private TankbookE2EDriver CreateDriver()
    {
        return new TankbookE2EDriver(() => MainWindow, SetFieldValue, WaitUntil, NavigateToTab);
    }
}
