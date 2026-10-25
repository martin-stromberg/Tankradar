using System.Xml.Linq;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft per XAML-Auswertung, dass jede Seite einen Titel für die Navigationsleiste trägt (Sprachausgabe und Windows-Kopfzeile)
/// und dass dieser Titel nicht noch einmal als Überschrift im Seiteninhalt steht.
/// </summary>
public class PageTitleRedundancyTests : BaseTest
{
    private static readonly XNamespace Maui = "http://schemas.microsoft.com/dotnet/2021/maui";

    /// <summary>
    /// Liefert die Namen aller Seiten-XAML-Dateien des Projekts.
    /// </summary>
    /// <returns>Die Dateinamen als Testdaten.</returns>
    public static IEnumerable<object[]> PageFiles()
    {
        var views = MauiSourceFiles.PathOf("Views");
        return Directory.GetFiles(views, "*Page.xaml").Select(path => new object[] { Path.GetFileName(path) });
    }

    /// <summary>
    /// Prüft, dass die Seite einen Titel hat und kein Label im Seiteninhalt denselben Titel wiederholt.
    /// </summary>
    /// <param name="fileName">Der Dateiname der Seite.</param>
    [Theory]
    [MemberData(nameof(PageFiles))]
    public void Page_HasTitle_AndDoesNotRepeatItAsHeading(string fileName)
    {
        var root = XDocument.Load(MauiSourceFiles.PathOf("Views", fileName)).Root;
        Assert.NotNull(root);

        var title = root.Attribute("Title")?.Value;
        Assert.False(string.IsNullOrWhiteSpace(title), $"{fileName} hat keinen Titel für die Navigationsleiste.");

        var repeated = root.Descendants(Maui + "Label")
            .Where(label => label.Attribute("Text")?.Value == title)
            .ToList();
        Assert.Empty(repeated);

        var headlineIds = root.Descendants()
            .Select(element => element.Attributes().FirstOrDefault(a => a.Name.LocalName == "AutomationId")?.Value)
            .Where(id => id is not null && id.EndsWith(".Headline", StringComparison.Ordinal));
        Assert.Empty(headlineIds);
    }
}
