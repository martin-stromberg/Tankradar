using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Export;
using Tankradar.Tests.Unit.Unit.Support;
using static Tankradar.Tests.Unit.Unit.Support.TankbookExportTestData;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Inhalt des <see cref="TankbookPdfReportWriter"/>: Kopf, Kennzahlen aus den Rechnern der Auswertung, Monatskosten und Einträge.
/// </summary>
public class TankbookPdfReportWriterTests_Content : BaseTest
{
    private static PdfTestReader WriteReport(TankbookExportData data)
    {
        return new PdfTestReader(new TankbookPdfReportWriter().Write(data));
    }

    /// <summary>
    /// Prüft den Kopf mit Fahrzeug, Zeitraum, Erstellungsdatum und Zahl der Vorgänge.
    /// </summary>
    [Fact]
    public void Write_HasHeaderWithVehicleRangeAndCount()
    {
        var reader = WriteReport(Build(CreateSeries(4), new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), CreateVehicle("Käfer")));

        var text = reader.Pages[0];
        Assert.Contains("Tankbuch", text);
        Assert.Contains("Käfer · Diesel • Start 10.000 km", text);
        Assert.Contains("01.01.2026 bis 31.12.2026", text);
        Assert.Contains("19.10.2026", text);
        Assert.Contains("4", text);
    }

    /// <summary>
    /// Prüft, dass ohne Zeitraum „Alle Vorgänge“ steht.
    /// </summary>
    [Fact]
    public void Write_WithoutRange_ShowsAllEntries()
    {
        var reader = WriteReport(Build(CreateSeries(2)));

        Assert.Contains("Alle Vorgänge", reader.Pages[0]);
    }

    /// <summary>
    /// Prüft die Kennzahlen: Durchschnittsverbrauch, Kosten pro 100 km und Gesamtkosten stammen aus der Auswertung.
    /// </summary>
    [Fact]
    public void Write_ShowsKeyFiguresFromStatistics()
    {
        var reader = WriteReport(Build(CreateSeries(4)));

        var text = reader.Pages[0];
        Assert.Contains("Kennzahlen", text);
        Assert.Contains("Kraftstoff", text);
        Assert.Contains("8,00 L/100 km", text);
        Assert.Contains("12,00 € / 100 km", text);
        Assert.Contains("240,00 €", text);
    }

    /// <summary>
    /// Prüft die Monatskosten je Art.
    /// </summary>
    [Fact]
    public void Write_ShowsMonthlyCosts()
    {
        var reader = WriteReport(Build(CreateSeries(4)));

        var text = reader.Pages[0];
        Assert.Contains("Monatskosten Kraftstoff", text);
        Assert.Contains("Januar 2026", text);
        Assert.Contains("240,00 €", text);
    }

    /// <summary>
    /// Prüft die Einträge der Tabelle mit Datum, Tankstelle, Menge, Betrag, Kilometerstand und Verbrauch.
    /// </summary>
    [Fact]
    public void Write_ListsEntriesWithValues()
    {
        var entries = new[]
        {
            CreateEntry(1, new DateTime(2026, 3, 5, 7, 9, 0), 11_500, 40m, 74.36m, "Müller Süd", "Urlaub (Italien)"),
            CreateEntry(2, new DateTime(2026, 3, 20, 18, 45, 0), 12_000, 35.5m, 66m),
        };

        var text = WriteReport(Build(entries)).Pages[0];

        Assert.Contains("05.03.2026 07:09", text);
        Assert.Contains("Müller Süd", text);
        Assert.Contains("40,00 L", text);
        Assert.Contains("74,36 €", text);
        Assert.Contains("11.500 km", text);
        Assert.Contains("2,67 L/100 km*", text);
        Assert.Contains("7,10 L/100 km", text);
        Assert.Contains("Notiz: Urlaub (Italien)", text);
        Assert.Contains(text, line => line.StartsWith("* ab Startstand", StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft Teilbetankung ohne Verbrauch und Strom mit kWh.
    /// </summary>
    [Fact]
    public void Write_PartialAndElectricEntries_ShowBadgeAndUnit()
    {
        var entries = new[]
        {
            CreateEntry(1, new DateTime(2026, 3, 5, 7, 0, 0), 10_500, 40m, 60m, isPartial: true),
            CreateEntry(2, new DateTime(2026, 3, 6, 7, 0, 0), 10_600, 20m, 8m, kind: TankbookEntryKind.Electricity),
        };

        var text = WriteReport(Build(entries)).Pages[0];

        Assert.Contains("Teilbetankung", text);
        Assert.Contains("20,00 kWh", text);
        Assert.Contains("Strom", text);
    }

    /// <summary>
    /// Prüft den Hinweis zu den Kennzahlen im Zeitraum nur bei eingeschränktem Zeitraum.
    /// </summary>
    [Fact]
    public void Write_RangeNote_OnlyWithRange()
    {
        var withRange = WriteReport(Build(CreateSeries(3), new DateTime(2026, 1, 1), new DateTime(2026, 12, 31))).Pages[0];
        var withoutRange = WriteReport(Build(CreateSeries(3))).Pages[0];

        Assert.Contains(withRange, line => line.StartsWith("Die Kennzahlen umfassen", StringComparison.Ordinal));
        Assert.DoesNotContain(withoutRange, line => line.StartsWith("Die Kennzahlen umfassen", StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft ein Fahrzeug ohne berechenbaren Verbrauch: Der Bericht entsteht und nennt den fehlenden Wert.
    /// </summary>
    [Fact]
    public void Write_WithoutConsumption_StillProducesReport()
    {
        var reader = WriteReport(Build([CreateEntry(1, new DateTime(2026, 3, 5, 7, 0, 0), 10_500, isPartial: true)]));

        Assert.Contains("noch nicht berechenbar", reader.Pages[0]);
        Assert.StartsWith("%PDF-1.4", reader.Text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass nicht abbildbare Zeichen und Klammern den Bericht nicht beschädigen.
    /// </summary>
    [Fact]
    public void Write_UnmappableAndSpecialCharacters_AreEscaped()
    {
        var reader = WriteReport(Build([CreateEntry(1, new DateTime(2026, 3, 5, 7, 0, 0), 10_500, station: "Шелл (Nord) \\ 😀")]));

        Assert.Contains("???? (Nord) \\ ?", reader.Pages[0]);
        Assert.Equal("Seite 1 von 1", reader.Pages[0][^1]);
    }

    /// <summary>
    /// Prüft, dass der Titel des Dokuments den Fahrzeugnamen trägt.
    /// </summary>
    [Fact]
    public void Write_InfoTitleContainsVehicleName()
    {
        var reader = WriteReport(Build(CreateSeries(1), vehicle: CreateVehicle("Golf")));

        Assert.Contains("/Title (Tankbuch Golf)", reader.Objects[5], StringComparison.Ordinal);
    }
}
