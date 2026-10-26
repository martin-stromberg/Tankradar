using System.Reflection;
using System.Xml.Linq;
using Tankradar.MAUI.ViewModels;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft per XAML-Auswertung, dass jede Seite einen Titel für die Navigationsleiste trägt (Sprachausgabe und Windows-Kopfzeile)
/// und dass dieser Titel nicht noch einmal als Überschrift im Seiteninhalt steht. <c>{x:Static}</c>-Verweise werden per Reflexion aufgelöst.
/// </summary>
public class PageTitleRedundancyTests : BaseTest
{
    private static readonly XNamespace Maui = "http://schemas.microsoft.com/dotnet/2021/maui";
    private const string ClrPrefix = "clr-namespace:";

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
    /// Prüft, dass die Seite einen Titel hat und kein Label oder Span im Seiteninhalt denselben Titel wiederholt.
    /// </summary>
    /// <param name="fileName">Der Dateiname der Seite.</param>
    [Theory]
    [MemberData(nameof(PageFiles))]
    public void Page_HasTitle_AndDoesNotRepeatItAsHeading(string fileName)
    {
        var document = XDocument.Load(MauiSourceFiles.PathOf("Views", fileName));
        Assert.NotNull(document.Root);

        Assert.False(string.IsNullOrWhiteSpace(ResolveTitle(document)), $"{fileName} hat keinen Titel für die Navigationsleiste.");
        Assert.Empty(FindRepeatedTitles(document));
    }

    /// <summary>
    /// Prüft, dass wörtliche und per <c>x:Static</c> angegebene Wiederholungen des Titels (auch in einem Span) erkannt werden.
    /// </summary>
    [Fact]
    public void FindRepeatedTitles_DetectsLiteralStaticAndSpanRepetitions()
    {
        const string xaml = """
            <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                         xmlns:texts="clr-namespace:Tankradar.MAUI.Resources.Texts"
                         Title="{x:Static texts:TankbookTexts.PageTitle}">
                <VerticalStackLayout>
                    <Label Text="tankbuch " />
                    <Label Text="{x:Static texts:TankbookTexts.PageTitle}" />
                    <Label><Label.FormattedText><FormattedString><Span Text="Tankbuch" /></FormattedString></Label.FormattedText></Label>
                    <Label Text="Fahrzeuge" />
                </VerticalStackLayout>
            </ContentPage>
            """;

        var repeated = FindRepeatedTitles(XDocument.Parse(xaml));

        Assert.Equal(3, repeated.Count);
    }

    /// <summary>
    /// Prüft, dass eine Seite ohne Wiederholung keine Treffer liefert.
    /// </summary>
    [Fact]
    public void FindRepeatedTitles_WithoutRepetition_ReturnsNothing()
    {
        const string xaml = """
            <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui" Title="Karte">
                <Label Text="Ansicht" />
            </ContentPage>
            """;

        Assert.Empty(FindRepeatedTitles(XDocument.Parse(xaml)));
    }

    private static List<string> FindRepeatedTitles(XDocument document)
    {
        var title = ResolveTitle(document);
        var found = new List<string>();
        if (string.IsNullOrWhiteSpace(title))
        {
            return found;
        }

        foreach (var element in document.Root!.Descendants().Where(e => e.Name == Maui + "Label" || e.Name == Maui + "Span"))
        {
            var text = Resolve(element.Attribute("Text")?.Value, element);
            if (string.Equals(text?.Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                found.Add(text!);
            }
        }

        found.AddRange(document.Root.Descendants()
            .Select(e => e.Attributes().FirstOrDefault(a => a.Name.LocalName == "AutomationId")?.Value)
            .Where(id => id is not null && id.EndsWith(".Headline", StringComparison.Ordinal))!);
        return found;
    }

    private static string? ResolveTitle(XDocument document)
    {
        return Resolve(document.Root!.Attribute("Title")?.Value, document.Root);
    }

    private static string? Resolve(string? value, XElement context)
    {
        if (value is null || !value.StartsWith("{x:Static ", StringComparison.Ordinal))
        {
            return value;
        }

        var reference = value["{x:Static ".Length..].TrimEnd('}').Trim();
        var parts = reference.Split(':');
        Assert.Equal(2, parts.Length);
        var member = parts[1].Split('.');
        Assert.Equal(2, member.Length);

        var ns = context.Document!.Root!.Attributes().Single(a => a.IsNamespaceDeclaration && a.Name.LocalName == parts[0]).Value;
        Assert.StartsWith(ClrPrefix, ns, StringComparison.Ordinal);
        var type = typeof(BaseViewModel).Assembly.GetType(ns[ClrPrefix.Length..].Split(';')[0] + "." + member[0]);
        Assert.NotNull(type);
        var field = type.GetField(member[1], BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(field);
        return (string?)field.GetRawConstantValue();
    }
}
