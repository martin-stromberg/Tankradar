using System.Text;
using Tankradar.MAUI.Services.Export;
using static Tankradar.Tests.Unit.Unit.Support.TankbookExportTestData;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Format der CSV-Datei des <see cref="TankbookCsvWriter"/> (ADR 0013): BOM, Kopfzeile, Trennzeichen, Zeilenende, Zahlen, Einheiten.
/// </summary>
public class TankbookCsvWriterTests_Format : BaseTest
{
    private static string[] ReadLines(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes).TrimStart('﻿').Split("\r\n", StringSplitOptions.None);
    }

    /// <summary>
    /// Prüft, dass die Datei mit dem UTF-8-BOM beginnt.
    /// </summary>
    [Fact]
    public void Write_StartsWithUtf8Bom()
    {
        var bytes = TankbookCsvWriter.Write(Build(CreateSeries(2)));

        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
    }

    /// <summary>
    /// Prüft die Kopfzeile mit allen Spalten, getrennt durch Semikolon, und das Zeilenende CRLF.
    /// </summary>
    [Fact]
    public void Write_HasHeaderWithSemicolonAndCrLf()
    {
        var text = Encoding.UTF8.GetString(TankbookCsvWriter.Write(Build(CreateSeries(2))));

        Assert.StartsWith("﻿Datum;Uhrzeit;Tankstelle;Art;Menge;Einheit;Preis pro Einheit (EUR);Gesamtbetrag (EUR);Kilometerstand;Teilbetankung;Strecke (km);Verbrauch;Verbrauchseinheit;Hinweis zum Verbrauch;Notiz\r\n", text);
        Assert.EndsWith("\r\n", text);
        Assert.DoesNotContain("\n", text.Replace("\r\n", string.Empty, StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft eine Zeile: Datum, Uhrzeit, Dezimalkomma ohne Tausendertrenner, Einheit Liter, Verbrauch.
    /// </summary>
    [Fact]
    public void Write_FuelRow_UsesGermanNumbersAndLiter()
    {
        var entries = new[]
        {
            CreateEntry(1, new DateTime(2026, 3, 5, 7, 9, 0), 11_500, 40m, 74.36m, "Müller Süd"),
            CreateEntry(2, new DateTime(2026, 3, 20, 18, 45, 0), 12_000, 35.5m, 66m),
        };

        var lines = ReadLines(TankbookCsvWriter.Write(Build(entries)));

        Assert.Equal("05.03.2026;07:09;Müller Süd;Kraftstoff;40,00;Liter;1,859;74,36;11500;Nein;1500;2,67;L/100 km;ab Startstand;", lines[1]);
        Assert.Equal("20.03.2026;18:45;Alpha Tankstelle;Kraftstoff;35,50;Liter;1,859;66,00;12000;Nein;500;7,10;L/100 km;;", lines[2]);
    }

    /// <summary>
    /// Prüft, dass Umlaute in UTF-8 erhalten bleiben.
    /// </summary>
    [Fact]
    public void Write_Umlauts_SurviveUtf8RoundTrip()
    {
        var bytes = TankbookCsvWriter.Write(Build([CreateEntry(1, new DateTime(2026, 3, 5, 7, 9, 0), 10_500, station: "Große Tankstelle Süd")]));

        Assert.Contains("Große Tankstelle Süd", Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft Teilbetankung (Ja, ohne Verbrauch) und Strom (kWh, Verbrauchseinheit).
    /// </summary>
    [Fact]
    public void Write_PartialAndElectricRows_HaveUnitsAndFlags()
    {
        var entries = new[]
        {
            CreateEntry(1, new DateTime(2026, 3, 5, 7, 0, 0), 10_500, 40m, 60m, isPartial: true),
            CreateEntry(2, new DateTime(2026, 3, 6, 7, 0, 0), 10_600, 20m, 8m, kind: MAUI.Models.Tankbook.TankbookEntryKind.Electricity),
        };

        var lines = ReadLines(TankbookCsvWriter.Write(Build(entries)));

        Assert.Equal("05.03.2026;07:00;Alpha Tankstelle;Kraftstoff;40,00;Liter;1,50;60,00;10500;Ja;;;;;", lines[1]);
        Assert.Equal("06.03.2026;07:00;Alpha Tankstelle;Strom;20,00;kWh;0,40;8,00;10600;Nein;;;;;", lines[2]);
    }

    /// <summary>
    /// Prüft, dass die Vorgänge chronologisch aufsteigend stehen, auch bei absteigender Eingabe.
    /// </summary>
    [Fact]
    public void Write_RowsAreAscending()
    {
        var lines = ReadLines(TankbookCsvWriter.Write(Build(CreateSeries(4))));

        Assert.Equal(6, lines.Length);
        Assert.StartsWith("03.01.2026", lines[1], StringComparison.Ordinal);
        Assert.StartsWith("09.01.2026", lines[4], StringComparison.Ordinal);
    }
}
