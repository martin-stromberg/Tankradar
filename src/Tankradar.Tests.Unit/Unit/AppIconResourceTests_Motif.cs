namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass das App-Icon (Hintergrund und Vordergrund) und der Startbildschirm das Tankatlas-Motiv tragen
/// und im Projekt als Icon- bzw. Splash-Ressource eingebunden sind, damit der MAUI-Resizetizer die Plattformgrößen erzeugt.
/// </summary>
public class AppIconResourceTests_Motif : BaseTest
{
    private static string ReadMauiFile(params string[] relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Tankradar.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine([directory.FullName, "src", "Tankradar.MAUI", .. relativePath]));
    }

    /// <summary>
    /// Prüft, dass der Hintergrund ein vollflächiger Verlauf von Teal nach Grün ohne abgerundete Ecken ist
    /// (iOS maskiert die Ecken selbst und verlangt ein App-Store-Icon ohne Transparenz).
    /// </summary>
    [Fact]
    public void Background_IsFullBleedGradientWithoutRoundedCorners()
    {
        var svg = ReadMauiFile("Resources", "AppIcon", "appicon.svg");

        Assert.Contains("<linearGradient", svg, StringComparison.Ordinal);
        Assert.Contains("#0D9488", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#10B981", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<rect x=\"0\" y=\"0\" width=\"456\" height=\"456\" fill=\"url(#navGrad)\"", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("rx=", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("opacity", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#512BD4", svg, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Prüft, dass Vordergrund und Splash dasselbe Motiv (weiße Zapfsäule, gelber Blitz) zeigen und der Vordergrund transparent bleibt.
    /// </summary>
    /// <param name="relativePath">Pfad der SVG-Datei unterhalb von Resources.</param>
    [Theory]
    [InlineData("AppIcon/appiconfg.svg")]
    [InlineData("Splash/splash.svg")]
    public void Motif_ShowsPumpAndBolt(string relativePath)
    {
        var svg = ReadMauiFile(["Resources", .. relativePath.Split('/')]);

        Assert.Contains("#FFFFFF", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#FEF08A", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<polygon", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("<rect x=\"0\" y=\"0\"", svg, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass das Projekt Icon (mit Vordergrund) und Splash über die MAUI-Elemente einbindet und nicht mehr die Vorlage verwendet.
    /// </summary>
    [Fact]
    public void Project_DeclaresIconAndSplash()
    {
        var project = ReadMauiFile("Tankradar.MAUI.csproj");

        Assert.Contains("<MauiIcon Include=\"Resources\\AppIcon\\appicon.svg\" ForegroundFile=\"Resources\\AppIcon\\appiconfg.svg\"", project, StringComparison.Ordinal);
        Assert.Contains("<MauiSplashScreen Include=\"Resources\\Splash\\splash.svg\"", project, StringComparison.Ordinal);
    }
}
