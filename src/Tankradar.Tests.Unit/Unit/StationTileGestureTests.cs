using System.Xml.Linq;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft per XAML-Auswertung, dass die Tankstellenkachel und der Gruppeneintrag eine echte Zeigerbedienung
/// (<see cref="Microsoft.Maui.Controls.TapGestureRecognizer"/>) tragen und nicht allein auf der transparenten
/// Overlay-Schaltfläche beruhen (Issue #44).
/// </summary>
public class StationTileGestureTests : BaseTest
{
    private static readonly XNamespace Maui = "http://schemas.microsoft.com/dotnet/2021/maui";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2009/xaml";

    /// <summary>
    /// Prüft, dass die Tankstellenkachel einen an <c>OpenCommand</c> gebundenen <see cref="Microsoft.Maui.Controls.TapGestureRecognizer"/> trägt.
    /// </summary>
    [Fact]
    public void StationTile_TileBorder_HasOpenTapGesture()
    {
        var document = XDocument.Load(MauiSourceFiles.PathOf("Views", "Controls", "StationTile.xaml"));

        var border = document.Root!.Descendants(Maui + "Border").Single(b => b.Attribute(Xaml + "Name")?.Value == "TileBorder");
        AssertHasOpenCommandGesture(border);
    }

    /// <summary>
    /// Prüft, dass der Stationsname der Gruppenansicht einen an <c>OpenCommand</c> gebundenen <see cref="Microsoft.Maui.Controls.TapGestureRecognizer"/> trägt.
    /// </summary>
    [Fact]
    public void FavoriteGroupPage_StationNameGrid_HasOpenTapGesture()
    {
        var document = XDocument.Load(MauiSourceFiles.PathOf("Views", "FavoriteGroupPage.xaml"));

        var name = document.Root!.Descendants(Maui + "Label").Single(l => l.Attribute("AutomationId")?.Value == "FavoriteGroup.Entry.Name");
        var grid = name.Ancestors(Maui + "Grid").First();
        AssertHasOpenCommandGesture(grid);
    }

    private static void AssertHasOpenCommandGesture(XElement element)
    {
        var gestures = element.Element(element.Name.Namespace + $"{element.Name.LocalName}.GestureRecognizers");
        Assert.NotNull(gestures);
        var recognizers = gestures.Elements(element.Name.Namespace + "TapGestureRecognizer").ToList();
        Assert.Contains(recognizers, recognizer => recognizer.Attribute("Command")?.Value == "{Binding OpenCommand}");
    }
}
