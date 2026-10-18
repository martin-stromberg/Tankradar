using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Formatierungen von <see cref="TankbookEntryTexts"/> (de-DE): Beträge, Einheiten, Datum, Verbrauchszeilen und Bezeichnungen.
/// </summary>
public class TankbookEntryTextsTests_Format : BaseTest
{
    private static readonly TankbookEntry Entry = new(1, 1, new DateTime(2026, 5, 18, 14, 30, 0), TankbookEntryKind.Fuel, 40m, 1.859m, 74.36m, 10_500, false, "Alpha Tankstelle", null, null, null);

    /// <summary>
    /// Prüft Beträge, Preise, Mengen, Kilometer, Verbrauch und Datum.
    /// </summary>
    [Fact]
    public void Format_Amounts_Units_Dates()
    {
        Assert.Equal("74,36 €", TankbookEntryTexts.FormatAmount(74.36m));
        Assert.Equal("1.234,50 €", TankbookEntryTexts.FormatAmount(1234.5m));
        Assert.Equal("1,859 €/L", TankbookEntryTexts.FormatUnitPrice(1.859m, TankbookEntryKind.Fuel));
        Assert.Equal("0,59 €/kWh", TankbookEntryTexts.FormatUnitPrice(0.59m, TankbookEntryKind.Electricity));
        Assert.Equal("40,00 L", TankbookEntryTexts.FormatQuantity(40m, TankbookEntryKind.Fuel));
        Assert.Equal("12,345 kWh", TankbookEntryTexts.FormatQuantity(12.345m, TankbookEntryKind.Electricity));
        Assert.Equal("10.500 km", TankbookEntryTexts.FormatKilometers(10_500));
        Assert.Equal("6,00 L/100 km", TankbookEntryTexts.FormatConsumption(6m, TankbookEntryKind.Fuel));
        Assert.Equal("16,50 kWh/100 km", TankbookEntryTexts.FormatConsumption(16.5m, TankbookEntryKind.Electricity));
        Assert.Equal("18.05.2026, 14:30", TankbookEntryTexts.FormatDateTime(Entry.OccurredAt));
        Assert.Equal("40,00", TankbookEntryTexts.FormatDecimalInput(40m));
        Assert.Equal("1,859", TankbookEntryTexts.FormatDecimalInput(1.859m));
        Assert.Equal("74,30", TankbookEntryTexts.FormatAmountInput(74.3m));
        Assert.Equal("Ergibt 74,36 €", TankbookEntryTexts.FormatComputedHint(AmountField.TotalAmount, 74.36m, TankbookEntryKind.Fuel));
        Assert.Equal("Ergibt 0,59 €/kWh", TankbookEntryTexts.FormatComputedHint(AmountField.UnitPrice, 0.59m, TankbookEntryKind.Electricity));
        Assert.Equal("Ergibt 30,00 L", TankbookEntryTexts.FormatComputedHint(AmountField.Quantity, 30m, TankbookEntryKind.Fuel));
        Assert.Equal("Letzter Stand 10.500 km", TankbookEntryTexts.FormatOdometerPlaceholder(10_500));
        Assert.Equal("„Urlaub“", TankbookEntryTexts.FormatNote(" Urlaub "));
        Assert.Equal(string.Empty, TankbookEntryTexts.FormatNote(null));
    }

    /// <summary>
    /// Prüft die Verbrauchszeile je Zustand.
    /// </summary>
    [Fact]
    public void ConsumptionText_ByState()
    {
        Assert.Equal("Verbrauch 6,00 L/100 km", TankbookEntryTexts.GetConsumptionLine(new TankbookEntryOverview(Entry, 500, 6m, TankbookConsumptionState.Calculated)));
        Assert.Equal("Teilbetankung: zählt beim nächsten vollen Vorgang mit", TankbookEntryTexts.GetConsumptionLine(new TankbookEntryOverview(Entry, null, null, TankbookConsumptionState.Partial)));
        Assert.Equal("Noch kein Verbrauch: erster voller Vorgang", TankbookEntryTexts.GetConsumptionLine(new TankbookEntryOverview(Entry, null, null, TankbookConsumptionState.NoPreviousFullEntry)));
        Assert.Equal("Kein Verbrauch: keine Strecke seit dem letzten vollen Vorgang", TankbookEntryTexts.GetConsumptionLine(new TankbookEntryOverview(Entry, 0, null, TankbookConsumptionState.NoDistance)));
        Assert.Equal("Strecke 500 km", TankbookEntryTexts.FormatDistanceLine(500));
        Assert.Equal("Menge 40,00 L", TankbookEntryTexts.FormatQuantityLine(Entry));
    }

    /// <summary>
    /// Prüft Bezeichnungen, Hinweise, Rückfrage und Beschriftungen der Arten und bekannten Tankstellen.
    /// </summary>
    [Fact]
    public void Labels_HintsAndQuestion()
    {
        Assert.Equal("Kraftstoff", TankbookEntryTexts.GetKindLabel(TankbookEntryKind.Fuel));
        Assert.Equal("Strom", TankbookEntryTexts.GetKindLabel(TankbookEntryKind.Electricity));
        Assert.Throws<ArgumentOutOfRangeException>(() => TankbookEntryTexts.GetKindLabel((TankbookEntryKind)99));
        Assert.Equal("Preis pro Liter", TankbookEntryTexts.GetUnitPriceLabel(TankbookEntryKind.Fuel));
        Assert.Equal("Vorgang vom 18.05.2026, 14:30, Alpha Tankstelle", TankbookEntryTexts.FormatEntryHint(Entry));
        Assert.Equal("Vorgang vom 18.05.2026, 14:30, Alpha Tankstelle endgültig löschen", TankbookEntryTexts.FormatConfirmDeleteHint(Entry));
        Assert.Equal("Beleg des Vorgangs vom 18.05.2026, 14:30", TankbookEntryTexts.FormatReceiptHint(Entry));
        Assert.Equal("Vorgang vom 18.05.2026, 14:30 (Alpha Tankstelle) löschen? Das lässt sich nicht rückgängig machen.", TankbookEntryTexts.FormatDeleteQuestion(Entry));
        Assert.Equal("Alpha Tankstelle", TankbookEntryTexts.FormatKnownStationLabel(new KnownStation("id", "Alpha Tankstelle", "Berlin"), false));
        Assert.Equal("Alpha Tankstelle (Berlin)", TankbookEntryTexts.FormatKnownStationLabel(new KnownStation("id", "Alpha Tankstelle", "Berlin"), true));
        Assert.Equal("Zuhause", TankbookEntryTexts.FormatKnownStationLabel(new KnownStation(null, "Zuhause", null), true));
        Assert.Equal("JET · Hauptstr. 1", TankbookEntryTexts.FormatKnownStationLabel(new KnownStation("id", "JET", "Berlin", "Hauptstr. 1"), false));
        Assert.Equal("JET · Hauptstr. 1 (Berlin)", TankbookEntryTexts.FormatKnownStationLabel(new KnownStation("id", "JET", "Berlin", "Hauptstr. 1"), true));
    }

    /// <summary>
    /// Prüft, dass gleichnamige bekannte Tankstellen zuerst durch das Unterscheidungsmerkmal, dann zusätzlich durch den Ort unterscheidbar werden.
    /// </summary>
    [Fact]
    public void KnownStationLabels_DisambiguateByDetailThenPlace()
    {
        Assert.Equal(
            ["Alpha", "Zuhause"],
            TankbookEntryTexts.FormatKnownStationLabels([new KnownStation("a", "Alpha", "Berlin", "Weg 1"), new KnownStation("b", "Zuhause", "Berlin")]));
        Assert.Equal(
            ["JET · JET BERLIN HAUPTSTR. 1", "JET · JET BERLIN NEBENWEG 22", "Shell"],
            TankbookEntryTexts.FormatKnownStationLabels(
            [
                new KnownStation("a", "JET", "Berlin", "JET BERLIN HAUPTSTR. 1"),
                new KnownStation("b", "JET", "Berlin", "JET BERLIN NEBENWEG 22"),
                new KnownStation("c", "Shell", "Berlin", "Weg 1"),
            ]));
        Assert.Equal(
            ["JET · Weg 1 (Berlin)", "JET · Weg 1 (Potsdam)", "JET · Ring 2"],
            TankbookEntryTexts.FormatKnownStationLabels(
            [
                new KnownStation("a", "JET", "Berlin", "Weg 1"),
                new KnownStation("b", "JET", "Potsdam", "Weg 1"),
                new KnownStation("c", "JET", "Potsdam", "Ring 2"),
            ]));
    }
}
