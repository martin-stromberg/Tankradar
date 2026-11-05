namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass Vordergrund und Startbildschirm das Tankatlas-Motiv (weiße Zapfsäule, gelber Blitz) tragen.
/// Aufbau, Deckung und Projekteinbindung der Icon-Quellen prüft <see cref="AppIconResourceTests_Xml"/> per XML-Auswertung.
/// </summary>
public class AppIconResourceTests_Motif : BaseTest
{
    /// <summary>
    /// Prüft, dass Vordergrund und Splash dasselbe Motiv (weiße Zapfsäule, gelber Blitz) zeigen und der Vordergrund transparent bleibt.
    /// </summary>
    /// <param name="relativePath">Pfad der SVG-Datei unterhalb von Resources.</param>
    [Theory]
    [InlineData("AppIcon/appiconfg.svg")]
    [InlineData("Splash/splash.svg")]
    public void Motif_ShowsPumpAndBolt(string relativePath)
    {
        var svg = MauiSourceFiles.Read(["Resources", .. relativePath.Split('/')]);

        Assert.Contains("#FFFFFF", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#FEF08A", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<polygon", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("<rect x=\"0\" y=\"0\"", svg, StringComparison.Ordinal);
    }
}
