using System.Text;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Export;
using static Tankradar.Tests.Unit.Unit.Support.TankbookExportTestData;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Randfälle des <see cref="TankbookCsvWriter"/>: leere Eingabe und Hybridfahrzeug mit beiden Arten.
/// </summary>
public class TankbookCsvWriterTests_Empty : BaseTest
{
    /// <summary>
    /// Prüft, dass eine leere Eingabe nur die Kopfzeile ergibt.
    /// </summary>
    [Fact]
    public void Write_NoEntries_ContainsOnlyHeader()
    {
        var text = Encoding.UTF8.GetString(TankbookCsvWriter.Write(Build([])));

        var lines = text.TrimStart('﻿').Split("\r\n", StringSplitOptions.None);
        Assert.Equal(["Datum;Uhrzeit;Tankstelle;Art;Menge;Einheit;Preis pro Einheit (EUR);Gesamtbetrag (EUR);Kilometerstand;Teilbetankung;Strecke (km);Verbrauch;Verbrauchseinheit;Hinweis zum Verbrauch;Notiz", string.Empty], lines);
    }

    /// <summary>
    /// Prüft, dass jede Zeile eines Hybridfahrzeugs ihre Art und Einheit trägt.
    /// </summary>
    [Fact]
    public void Write_Hybrid_EveryRowCarriesKindAndUnit()
    {
        var entries = new[]
        {
            CreateEntry(1, new DateTime(2026, 3, 5, 7, 0, 0), 10_500, 40m, 60m),
            CreateEntry(2, new DateTime(2026, 3, 6, 7, 0, 0), 10_600, 20m, 8m, kind: TankbookEntryKind.Electricity),
            CreateEntry(3, new DateTime(2026, 3, 20, 7, 0, 0), 11_000, 30m, 45m),
            CreateEntry(4, new DateTime(2026, 3, 21, 7, 0, 0), 11_100, 10m, 4m, kind: TankbookEntryKind.Electricity),
        };

        var lines = Encoding.UTF8.GetString(TankbookCsvWriter.Write(Build(entries, vehicle: CreateVehicle("Plug-in", VehicleFuelKind.Hybrid)))).TrimStart('﻿').Split("\r\n");

        Assert.Contains(";Kraftstoff;", lines[1], StringComparison.Ordinal);
        Assert.Contains(";Liter;", lines[1], StringComparison.Ordinal);
        Assert.Contains(";Strom;", lines[2], StringComparison.Ordinal);
        Assert.Contains(";kWh;", lines[2], StringComparison.Ordinal);
        Assert.EndsWith(";kWh/100 km;;", lines[4], StringComparison.Ordinal);
    }
}
