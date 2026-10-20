using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Export;
using Tankradar.Tests.Unit.Unit.Support;
using static Tankradar.Tests.Unit.Unit.Support.TankbookExportTestData;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Layout des <see cref="TankbookPdfReportWriter"/>: Texte auf derselben Grundlinie überlappen sich nicht.
/// </summary>
public class TankbookPdfReportWriterTests_Layout : BaseTest
{
    private static List<TankbookEntry> CreateMixedSeries()
    {
        var entries = new List<TankbookEntry>();
        for (var index = 1; index <= 4; index++)
        {
            entries.Add(CreateEntry(index, new DateTime(2026, 1, 1, 8, 0, 0).AddDays(index * 2), 10_000 + (index * 500), 40m, 60m, $"Station {index}"));
            entries.Add(CreateEntry(100 + index, new DateTime(2026, 1, 1, 9, 0, 0).AddDays(index * 2), 10_000 + (index * 500) + 10, 20m, 8m, kind: TankbookEntryKind.Electricity));
        }

        return entries;
    }

    private static void AssertNoOverlap(byte[] pdf)
    {
        var reader = new PdfTestReader(pdf);
        for (var page = 0; page < reader.PageStreams.Count; page++)
        {
            foreach (var row in reader.ReadPlacedTexts(page).GroupBy(text => text.Y))
            {
                var ordered = row.OrderBy(text => text.X).ToList();
                for (var index = 1; index < ordered.Count; index++)
                {
                    var previous = ordered[index - 1];
                    Assert.True(
                        previous.X + previous.Width <= ordered[index].X + 0.01,
                        $"Seite {page + 1}: „{previous.Text}“ (bis {previous.X + previous.Width:0.0} pt) überlappt „{ordered[index].Text}“ (ab {ordered[index].X:0.0} pt).");
                }
            }
        }
    }

    /// <summary>
    /// Prüft, dass Beschriftung und Wert der Kennzahlen bei Dieselfahrzeugen nicht überlappen.
    /// </summary>
    [Fact]
    public void Write_KeyFigures_DoNotOverlap_Fuel()
    {
        AssertNoOverlap(TankbookPdfReportWriter.Write(Build(CreateSeries(5))));
    }

    /// <summary>
    /// Prüft das auch für Elektrofahrzeuge und mit Zeitraum.
    /// </summary>
    [Fact]
    public void Write_KeyFigures_DoNotOverlap_Electric()
    {
        var entries = Enumerable.Range(1, 4)
            .Select(index => CreateEntry(index, new DateTime(2026, 1, 1, 8, 0, 0).AddDays(index * 2), 10_000 + (index * 300), 50m, 15m, kind: TankbookEntryKind.Electricity))
            .ToList();

        AssertNoOverlap(TankbookPdfReportWriter.Write(Build(entries, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), CreateVehicle("Stromer", VehicleFuelKind.Electric))));
    }

    /// <summary>
    /// Prüft das auch für Hybridfahrzeuge mit Kraftstoff und Strom und mit einem sehr langen Fahrzeugnamen.
    /// </summary>
    [Fact]
    public void Write_KeyFigures_DoNotOverlap_Hybrid()
    {
        var vehicle = CreateVehicle(new string('W', 80), VehicleFuelKind.Hybrid);

        AssertNoOverlap(TankbookPdfReportWriter.Write(Build(CreateMixedSeries(), vehicle: vehicle)));
    }

    /// <summary>
    /// Prüft, dass die Wertspalte rechts von der breitesten Beschriftung beginnt, auch von „Strecke der berechneten Intervalle“.
    /// </summary>
    [Fact]
    public void Write_DistanceLabel_EndsBeforeValueColumn()
    {
        var reader = new PdfTestReader(TankbookPdfReportWriter.Write(Build(CreateSeries(5))));

        var texts = reader.ReadPlacedTexts(0);
        var label = texts.Single(text => text.Text == "Strecke der berechneten Intervalle");
        var value = texts.Single(text => text.Y == label.Y && text.Text != label.Text);
        Assert.True(label.X + label.Width < value.X);
    }
}
