using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Exceptions;
using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Bedient das Tankbuch der laufenden App (Fahrzeuge sowie Tank- und Ladevorgänge) ausschließlich über UI-Automation-Muster (kein Mausklick).
/// Als eigene Klasse (Komposition) nutzbar aus <see cref="TankbookE2ETestBase"/> und aus Testbasen mit Mock-Preisdienst (z. B. dem Offline-Szenario).
/// </summary>
public sealed class TankbookE2EDriver
{
    private readonly Func<Window> _window;
    private readonly Action<string, string> _setFieldValue;
    private readonly Action<Func<bool>, string> _waitUntil;
    private readonly Action<string, string?> _navigateToTab;

    /// <summary>
    /// Erstellt den Treiber.
    /// </summary>
    /// <param name="window">Liefert das Hauptfenster der aktuell laufenden App (auch nach einem Neustart).</param>
    /// <param name="setFieldValue">Trägt einen Text in ein Eingabefeld ein und wartet, bis die App ihn übernommen hat.</param>
    /// <param name="waitUntil">Wartet auf eine Bedingung und schlägt sonst mit der Meldung fehl.</param>
    /// <param name="navigateToTab">Wechselt in einen Reiter der App (Titel, AutomationId der Zielseite).</param>
    public TankbookE2EDriver(Func<Window> window, Action<string, string> setFieldValue, Action<Func<bool>, string> waitUntil, Action<string, string?> navigateToTab)
    {
        _window = window;
        _setFieldValue = setFieldValue;
        _waitUntil = waitUntil;
        _navigateToTab = navigateToTab;
    }

    /// <summary>
    /// Wechselt in den Bereich „Tankbuch“ und wartet, bis „Fahrzeug hinzufügen“ angezeigt wird.
    /// </summary>
    public void OpenTankbook()
    {
        _navigateToTab("Tankbuch", null);
        WaitFor("Tankbook.AddVehicle");
    }

    /// <summary>
    /// Sucht das erste Element mit der AutomationId; ein vorübergehender UIA-Timeout wird begrenzt wiederholt.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns>Das Element oder <see langword="null"/>.</returns>
    public AutomationElement? Find(string automationId)
    {
        return TransientRetry.Run(
            () => _window().FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            $"Suche des Elements '{automationId}'",
            maxAttempts: 3,
            pause: TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Wartet, bis ein Element mit der AutomationId vorhanden ist.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns>Das Element.</returns>
    public AutomationElement WaitFor(string automationId)
    {
        AutomationElement? found = null;
        _waitUntil(() => (found = Find(automationId)) is not null, $"Das Element '{automationId}' wurde nicht gefunden.");
        return found!;
    }

    /// <summary>
    /// Gibt an, ob ein Element mit der AutomationId aktuell existiert (unsichtbare Elemente fehlen im Baum).
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns><see langword="true"/>, wenn das Element existiert.</returns>
    public bool Exists(string automationId)
    {
        return Find(automationId) is not null;
    }

    /// <summary>
    /// Wartet auf eine Bedingung.
    /// </summary>
    /// <param name="condition">Die Bedingung.</param>
    /// <param name="failureMessage">Die Meldung bei Zeitüberschreitung.</param>
    public void WaitUntil(Func<bool> condition, string failureMessage)
    {
        _waitUntil(condition, failureMessage);
    }

    /// <summary>
    /// Löst die Schaltfläche mit der AutomationId aus.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    public void Press(string automationId)
    {
        WaitFor(automationId).Patterns.Invoke.Pattern.Invoke();
    }

    /// <summary>
    /// Trägt den Text in das Eingabefeld mit der AutomationId ein und wartet, bis die App ihn übernommen hat.
    /// </summary>
    /// <param name="automationId">Die AutomationId des Eingabefelds.</param>
    /// <param name="text">Der Text.</param>
    public void Type(string automationId, string text)
    {
        _setFieldValue(automationId, text);
    }

    /// <summary>
    /// Schaltet den Schalter mit der AutomationId in den gewünschten Zustand.
    /// </summary>
    /// <param name="automationId">Die AutomationId des Schalters.</param>
    /// <param name="on">Der gewünschte Zustand.</param>
    public void ToggleSwitch(string automationId, bool on)
    {
        var expected = on ? ToggleState.On : ToggleState.Off;
        var toggle = WaitFor(automationId).Patterns.Toggle.Pattern;
        if (toggle.ToggleState.Value != expected)
        {
            toggle.Toggle();
        }

        _waitUntil(() => Find(automationId)?.Patterns.Toggle.PatternOrDefault?.ToggleState.ValueOrDefault == expected, $"Der Schalter '{automationId}' steht nicht auf {expected}.");
    }

    /// <summary>
    /// Liefert die Elemente mit der AutomationId in Dokumentreihenfolge (bei senkrechten Listen von oben nach unten).
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns>Die Elemente.</returns>
    public IReadOnlyList<AutomationElement> FindAllOrdered(string automationId)
    {
        return _window().FindAllDescendants(cf => cf.ByAutomationId(automationId));
    }

    /// <summary>
    /// Liefert die Texte aller Elemente mit der AutomationId von oben nach unten; ein Element, das gerade neu aufgebaut wird, führt zu einer leeren Liste.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns>Die Texte.</returns>
    public IReadOnlyList<string> ReadTexts(string automationId)
    {
        try
        {
            return FindAllOrdered(automationId).Select(element => element.Name).ToList();
        }
        catch (Exception ex) when (ex is PropertyNotSupportedException or ElementNotAvailableException)
        {
            return [];
        }
    }

    /// <summary>
    /// Wartet, bis die Texte aller Elemente mit der AutomationId genau den erwarteten Werten in dieser Reihenfolge entsprechen.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <param name="expected">Die erwarteten Texte.</param>
    public void WaitForTexts(string automationId, params string[] expected)
    {
        var last = string.Empty;
        _waitUntil(
            () =>
            {
                var texts = ReadTexts(automationId);
                if (texts.Count > 0)
                {
                    last = string.Join(" | ", texts);
                }

                return texts.SequenceEqual(expected);
            },
            $"'{automationId}' zeigt nicht [{string.Join(" | ", expected)}], zuletzt gelesen: [{last}].");
    }

    /// <summary>
    /// Liefert den Hinweis (HelpText, aus <c>SemanticProperties.Hint</c>) des ersten Elements mit der AutomationId.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns>Der Hinweis.</returns>
    public string ReadHint(string automationId)
    {
        return WaitFor(automationId).Properties.HelpText.ValueOrDefault ?? string.Empty;
    }

    /// <summary>
    /// Löst die Schaltfläche mit der AutomationId im Eintrag des Fahrzeugs mit dem Namen aus (gleicher Listenplatz wie der Name).
    /// </summary>
    /// <param name="automationId">Die AutomationId der Schaltfläche im Eintrag.</param>
    /// <param name="vehicleName">Der Name des Fahrzeugs.</param>
    public void PressForVehicle(string automationId, string vehicleName)
    {
        PressAtIndex(automationId, () => ReadTexts("Tankbook.Vehicle.Name").ToList().IndexOf(vehicleName), $"des Fahrzeugs '{vehicleName}'");
    }

    /// <summary>
    /// Löst die Schaltfläche mit der AutomationId im Vorgang am Listenplatz aus (0 = neuester Vorgang).
    /// </summary>
    /// <param name="automationId">Die AutomationId der Schaltfläche im Eintrag.</param>
    /// <param name="index">Der Listenplatz.</param>
    public void PressForEntry(string automationId, int index)
    {
        PressAtIndex(automationId, () => index, $"des Vorgangs an Platz {index}");
    }

    /// <summary>
    /// Füllt das geöffnete Fahrzeugformular aus.
    /// </summary>
    /// <param name="name">Der Name.</param>
    /// <param name="fuelKindKey">Der Schlüssel der Kraftstoffart (Petrol, Diesel, Electric, Hybrid).</param>
    /// <param name="odometer">Der Start-Kilometerstand.</param>
    /// <param name="consumption">Der Startverbrauch (leer für keinen).</param>
    public void FillVehicleForm(string name, string fuelKindKey, string odometer, string consumption)
    {
        Type("Tankbook.Form.Name", name);
        Press($"Tankbook.Form.FuelKind.{fuelKindKey}");
        _waitUntil(
            () => Find($"Tankbook.Form.FuelKind.{fuelKindKey}")?.Properties.HelpText.ValueOrDefault == "Ausgewählt",
            $"Die Kraftstoffart '{fuelKindKey}' wurde nicht ausgewählt.");
        Type("Tankbook.Form.Odometer", odometer);
        Type("Tankbook.Form.Consumption", consumption);
    }

    /// <summary>
    /// Betätigt „Speichern“ im Fahrzeugformular und wartet, bis es geschlossen ist.
    /// </summary>
    public void SaveVehicleForm()
    {
        Press("Tankbook.Form.Save");
        _waitUntil(() => !Exists("Tankbook.Form.Save"), "Das Formular wurde nach dem Speichern nicht geschlossen.");
    }

    /// <summary>
    /// Legt ein Fahrzeug über „Fahrzeug hinzufügen“ an.
    /// </summary>
    /// <param name="name">Der Name.</param>
    /// <param name="fuelKindKey">Der Schlüssel der Kraftstoffart.</param>
    /// <param name="odometer">Der Start-Kilometerstand.</param>
    /// <param name="consumption">Der Startverbrauch (leer für keinen).</param>
    public void CreateVehicle(string name, string fuelKindKey, string odometer, string consumption)
    {
        Press("Tankbook.AddVehicle");
        WaitFor("Tankbook.Form.Name");
        FillVehicleForm(name, fuelKindKey, odometer, consumption);
        SaveVehicleForm();
        _waitUntil(() => ReadTexts("Tankbook.Vehicle.Name").Contains(name), $"Das Fahrzeug '{name}' erscheint nicht in der Liste.");
    }

    /// <summary>
    /// Öffnet über „Tank- und Ladevorgänge“ der Fahrzeugkarte die Vorgänge des Fahrzeugs und wartet auf die Seite.
    /// </summary>
    /// <param name="vehicleName">Der Name des Fahrzeugs.</param>
    public void OpenEntries(string vehicleName)
    {
        PressForVehicle("Tankbook.Vehicle.Entries", vehicleName);
        WaitForTexts("TankbookEntries.Vehicle", vehicleName);
        WaitFor("TankbookEntries.Add");
    }

    /// <summary>
    /// Kehrt mit dem Zurück-Pfeil der Kopfleiste zur Fahrzeugliste zurück.
    /// </summary>
    public void LeaveEntries()
    {
        Press("NavigationViewBackButton");
        WaitFor("Tankbook.AddVehicle");
    }

    /// <summary>
    /// Füllt das geöffnete Formular eines Vorgangs aus; leere Werte lassen das Feld leer (Datum und Uhrzeit bleiben vorbelegt).
    /// </summary>
    /// <param name="station">Der Name der Tankstelle bzw. des Ladeorts (leer: unverändert).</param>
    /// <param name="quantity">Die Menge.</param>
    /// <param name="unitPrice">Der Preis pro Einheit.</param>
    /// <param name="total">Der Gesamtbetrag.</param>
    /// <param name="odometer">Der Kilometerstand.</param>
    /// <param name="isPartial">Gibt an, ob der Schalter „Teilbetankung“ eingeschaltet wird.</param>
    public void FillEntryForm(string station, string quantity, string unitPrice, string total, string odometer, bool isPartial = false)
    {
        if (station.Length > 0)
        {
            Type("TankbookEntries.Form.Station", station);
        }

        Type("TankbookEntries.Form.Quantity", quantity);
        Type("TankbookEntries.Form.UnitPrice", unitPrice);
        Type("TankbookEntries.Form.TotalAmount", total);
        Type("TankbookEntries.Form.Odometer", odometer);
        ToggleSwitch("TankbookEntries.Form.Partial", isPartial);
    }

    /// <summary>
    /// Betätigt „Speichern“ im Formular eines Vorgangs und wartet, bis es geschlossen ist.
    /// </summary>
    public void SaveEntryForm()
    {
        Press("TankbookEntries.Form.Save");
        _waitUntil(() => !Exists("TankbookEntries.Form.Save"), "Das Formular des Vorgangs wurde nach dem Speichern nicht geschlossen.");
    }

    /// <summary>
    /// Erfasst einen Vorgang über „Vorgang erfassen“ und wartet, bis die Liste die erwartete Zahl an Vorgängen zeigt.
    /// </summary>
    /// <param name="station">Der Name der Tankstelle bzw. des Ladeorts.</param>
    /// <param name="quantity">Die Menge.</param>
    /// <param name="unitPrice">Der Preis pro Einheit.</param>
    /// <param name="total">Der Gesamtbetrag.</param>
    /// <param name="odometer">Der Kilometerstand.</param>
    /// <param name="isPartial">Gibt an, ob es eine Teilbetankung ist.</param>
    public void CreateEntry(string station, string quantity, string unitPrice, string total, string odometer, bool isPartial = false)
    {
        var before = ReadTexts("TankbookEntry.Item.Station").Count;
        Press("TankbookEntries.Add");
        WaitFor("TankbookEntries.Form.Station");
        FillEntryForm(station, quantity, unitPrice, total, odometer, isPartial);
        SaveEntryForm();
        _waitUntil(() => ReadTexts("TankbookEntry.Item.Station").Count == before + 1, "Der neue Vorgang erscheint nicht in der Liste.");
    }

    /// <summary>
    /// Liefert die Texte eines Felds aller Vorgänge (neueste zuerst), z. B. „TankbookEntry.Item.Consumption“.
    /// </summary>
    /// <param name="automationId">Die AutomationId des Felds im Eintrag.</param>
    /// <returns>Die Texte.</returns>
    public IReadOnlyList<string> ReadEntryTexts(string automationId)
    {
        return ReadTexts(automationId);
    }

    /// <summary>
    /// Liefert die AutomationId eines Elements der Auswertung einer Art.
    /// </summary>
    /// <param name="kindKey">Der Schlüssel der Art (<c>Fuel</c> oder <c>Electricity</c>).</param>
    /// <param name="element">Der Name des Elements, z. B. <c>Average</c> oder <c>CostChartSummary</c>.</param>
    /// <returns>Die AutomationId.</returns>
    public static string StatisticsId(string kindKey, string element)
    {
        return $"TankbookEntries.Statistics.{kindKey}.{element}";
    }

    /// <summary>
    /// Wartet, bis ein Element der Auswertung einer Art den erwarteten Text zeigt.
    /// </summary>
    /// <param name="kindKey">Der Schlüssel der Art.</param>
    /// <param name="element">Der Name des Elements.</param>
    /// <param name="expected">Der erwartete Text.</param>
    public void WaitForStatistic(string kindKey, string element, string expected)
    {
        WaitForTexts(StatisticsId(kindKey, element), expected);
    }

    /// <summary>
    /// Wartet, bis die Auswertung für die Art Kennzahlen zeigt (Durchschnitt, Kosten pro 100 km, Kosten des laufenden Monats) und prüft sie.
    /// Wert und Einheit der Kacheln stehen in getrennten Elementen; sie werden hier als ein Text mit Leerzeichen übergeben.
    /// </summary>
    /// <param name="kindKey">Der Schlüssel der Art.</param>
    /// <param name="average">Der erwartete Durchschnittsverbrauch mit Einheit, z. B. „6,00 L/100 km“.</param>
    /// <param name="costPer100Km">Die erwarteten Kosten pro 100 km mit Einheit, z. B. „11,15 €“.</param>
    /// <param name="monthCost">Die erwarteten Kosten des laufenden Monats.</param>
    public void WaitForStatistics(string kindKey, string average, string costPer100Km, string monthCost)
    {
        var (averageValue, averageUnit) = SplitValueAndUnit(average);
        var (costValue, costUnit) = SplitValueAndUnit(costPer100Km);
        WaitForStatistic(kindKey, "Average", averageValue);
        WaitForStatistic(kindKey, "AverageUnit", averageUnit);
        WaitForStatistic(kindKey, "CostPer100Km", costValue);
        WaitForStatistic(kindKey, "CostPer100KmUnit", costUnit);
        WaitForStatistic(kindKey, "MonthCost", monthCost);
    }

    private static (string Value, string Unit) SplitValueAndUnit(string text)
    {
        var separator = text.IndexOf(' ', StringComparison.Ordinal);
        return (text[..separator], text[(separator + 1)..]);
    }

    private void PressAtIndex(string automationId, Func<int> index, string description)
    {
        // Indexermittlung, Suche und Auslösen laufen gemeinsam im Wiederholungsblock: Baut sich die Liste zwischendurch neu auf, wird neu gesucht.
        _waitUntil(
            () =>
            {
                try
                {
                    var position = index();
                    var buttons = FindAllOrdered(automationId);
                    if (position < 0 || buttons.Count <= position)
                    {
                        return false;
                    }

                    buttons[position].Patterns.Invoke.Pattern.Invoke();
                    return true;
                }
                catch (Exception ex) when (ex is ElementNotAvailableException or PropertyNotSupportedException or InvalidOperationException)
                {
                    return false;
                }
            },
            $"Die Schaltfläche '{automationId}' {description} fehlt oder ließ sich nicht auslösen.");
    }
}
