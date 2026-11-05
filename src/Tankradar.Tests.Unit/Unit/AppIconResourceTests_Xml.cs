using System.Globalization;
using System.Xml.Linq;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Icon-Quellen (SVG) und ihre Einbindung per XML-Auswertung statt per Textsuche, damit Umformatierungen
/// (Zeilenumbrüche, Attributreihenfolge) die Tests nicht brechen, echte Fehler aber auffallen.
/// </summary>
public class AppIconResourceTests_Xml : BaseTest
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    private static XElement LoadSvg(string fileName)
    {
        var root = XDocument.Load(MauiSourceFiles.PathOf("Resources", "AppIcon", fileName)).Root;
        Assert.NotNull(root);
        Assert.Equal(Svg + "svg", root.Name);
        return root;
    }

    private static double Number(XElement element, string attribute)
    {
        var text = element.Attribute(attribute)?.Value;
        Assert.False(string.IsNullOrWhiteSpace(text), $"Attribut {attribute} fehlt.");
        return double.Parse(text, CultureInfo.InvariantCulture);
    }

    private static bool IsFullBleed(XElement rect, double size)
    {
        return Number(rect, "x") == 0 && Number(rect, "y") == 0 && Number(rect, "width") == size && Number(rect, "height") == size;
    }

    /// <summary>
    /// Prüft, dass der Hintergrund quadratisch ist, über die ganze Fläche (viewBox) reicht und mit einem Teal-Grün-Verlauf gefüllt ist.
    /// </summary>
    [Fact]
    public void Background_IsFullBleedOpaqueSquare()
    {
        var root = LoadSvg("appicon.svg");
        var size = Number(root, "width");
        Assert.Equal(size, Number(root, "height"));
        var sizeText = size.ToString(CultureInfo.InvariantCulture);
        Assert.Equal($"0 0 {sizeText} {sizeText}", root.Attribute("viewBox")?.Value);

        var rect = Assert.Single(root.Descendants(Svg + "rect"));
        Assert.True(IsFullBleed(rect, size));
        Assert.Equal("url(#navGrad)", rect.Attribute("fill")?.Value);

        var gradient = Assert.Single(root.Descendants(Svg + "linearGradient"));
        Assert.Equal("navGrad", gradient.Attribute("id")?.Value);
        var colors = gradient.Elements(Svg + "stop").Select(s => s.Attribute("stop-color")?.Value).ToList();
        Assert.Equal(["#0D9488", "#10B981"], colors, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Prüft, dass der Hintergrund weder abgerundete Ecken noch Transparenz enthält (iOS maskiert selbst, der App Store lehnt Transparenz ab).
    /// </summary>
    [Fact]
    public void Background_HasNoRoundedCornersOrTransparency()
    {
        var root = LoadSvg("appicon.svg");
        var attributes = root.DescendantsAndSelf().SelectMany(e => e.Attributes()).ToList();

        Assert.DoesNotContain(attributes, a => a.Name.LocalName is "rx" or "ry");
        Assert.DoesNotContain(attributes, a => a.Name.LocalName.Contains("opacity", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(attributes, a => a.Value.Contains("#512BD4", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Prüft, dass der Vordergrund dieselbe Größe wie der Hintergrund hat und kein vollflächiges Rechteck enthält (er muss transparent bleiben).
    /// </summary>
    [Fact]
    public void Foreground_MatchesBackgroundSizeAndHasNoFullSizeRect()
    {
        var background = LoadSvg("appicon.svg");
        var foreground = LoadSvg("appiconfg.svg");
        var size = Number(background, "width");

        Assert.Equal(size, Number(foreground, "width"));
        Assert.Equal(size, Number(foreground, "height"));
        Assert.DoesNotContain(foreground.Descendants(Svg + "rect"), rect => rect.Attribute("width") is not null && Number(rect, "width") >= size && Number(rect, "height") >= size);
    }

    /// <summary>
    /// Prüft, dass das Marketing-Icon (1024 px) aus einer quadratischen, vollflächig gefüllten Quelle entsteht: Die Größe stammt aus
    /// der Vektorgröße, das quadratische Seitenverhältnis verhindert Verzerrung, die vollflächige Füllung verhindert Transparenz.
    /// </summary>
    [Fact]
    public void MarketingIcon_SourceIsSquareAndFullyCovered()
    {
        var root = LoadSvg("appicon.svg");
        var size = Number(root, "width");

        Assert.Equal(size, Number(root, "height"));
        Assert.True(size >= 256, "Die Vektorgröße muss für die Skalierung auf 1024 px ausreichen.");
        Assert.All(root.Descendants(Svg + "rect"), rect => Assert.True(IsFullBleed(rect, size)));
    }

    /// <summary>
    /// Prüft, dass das Projekt Icon (mit Vordergrund) und Splash über die MAUI-Elemente einbindet und die Windows-ICO-Erzeugung importiert.
    /// </summary>
    [Fact]
    public void Project_DeclaresIconSplashAndWindowsIconTarget()
    {
        var project = XDocument.Load(MauiSourceFiles.PathOf("Tankradar.MAUI.csproj")).Root;
        Assert.NotNull(project);

        var icon = Assert.Single(project.Descendants("MauiIcon"));
        Assert.Equal("Resources\\AppIcon\\appicon.svg", icon.Attribute("Include")?.Value);
        Assert.Equal("Resources\\AppIcon\\appiconfg.svg", icon.Attribute("ForegroundFile")?.Value);
        var splash = Assert.Single(project.Descendants("MauiSplashScreen"));
        Assert.Equal("Resources\\Splash\\splash.svg", splash.Attribute("Include")?.Value);

        var import = Assert.Single(project.Descendants("Import"), i => i.Attribute("Project")?.Value == "MSBuild\\WindowsAppIcon.targets");
        Assert.Contains("windows", import.Attribute("Condition")?.Value, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass das MSBuild-Ziel für das Windows-Icon alle geforderten Größen (16 bis 256 px) einsammelt und nach dem Resizetizer läuft.
    /// </summary>
    [Fact]
    public void WindowsIconTarget_CollectsAllSizesAfterResizetizer()
    {
        var targets = XDocument.Load(MauiSourceFiles.PathOf("MSBuild", "WindowsAppIcon.targets")).Root;
        Assert.NotNull(targets);

        var target = Assert.Single(targets.Elements("Target"), t => t.Attribute("Name")?.Value == "TankradarMultiSizeWindowsIcon");
        Assert.Equal("ResizetizeImages", target.Attribute("AfterTargets")?.Value);

        var parts = target.Descendants("_TankradarIconPart").Select(p => p.Attribute("Include")?.Value ?? string.Empty).ToList();
        foreach (var size in new[] { 16, 24, 32, 48, 256 })
        {
            Assert.Contains(parts, part => part.EndsWith($"appiconLogo.targetsize-{size}.png", StringComparison.Ordinal));
        }

        Assert.Single(target.Descendants("WriteMultiSizeIcon"));
    }
}
