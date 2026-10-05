using System.Diagnostics;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Bei sehr vielen Treffern (300 zusätzliche Stationen im Mock) wird nur eine Seite dargestellt; Filter- und Sortierwechsel bleiben zügig.
/// </summary>
public class SearchE2ETests_LargeResult : SearchE2ETestBase
{
    private const int PageSize = 25;

    /// <summary>
    /// Prüft, dass nur die erste Seite angezeigt wird, „Weitere anzeigen“ die nächste ergänzt und ein Sortierwechsel zügig auf die erste Seite zurückführt.
    /// </summary>
    [Fact]
    public void LargeResult_ShowsOnePageAndSortChangeStaysFast()
    {
        Server.AddGeneratedStations(300);
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            Submit();
            WaitUntil(() => ReadTexts("Search.Station.Name").Count == PageSize, "Die erste Seite der Ergebnisliste wurde nicht angezeigt.");
            Assert.True(Exists("Search.ShowMore"));

            FindAllOrdered("Search.ShowMore").Single().Patterns.Invoke.Pattern.Invoke();
            WaitUntil(() => ReadTexts("Search.Station.Name").Count == 2 * PageSize, "Die zweite Seite wurde nicht angezeigt.");

            var stopwatch = Stopwatch.StartNew();
            SelectSort("Name");
            WaitUntil(() => ReadTexts("Search.Station.Name").Count == PageSize, "Nach dem Sortierwechsel wird nicht die erste Seite angezeigt.");
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"Der Sortierwechsel dauerte {stopwatch.Elapsed.TotalSeconds:0.0} s.");
            Assert.Equal(1, Server.ListRequests);
        });
    }
}
