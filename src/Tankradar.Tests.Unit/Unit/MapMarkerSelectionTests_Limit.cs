using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Map;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Auswahl der Markierungen, die die Karte tatsächlich aufbaut: nur Stationen im Ausschnitt, höchstens eine Obergrenze, nächste zur Mitte zuerst.
/// Regression: Für alle Stationen einer großen Ergebnismenge wurden Schaltflächen erzeugt und der UI-Automation-Baum dadurch riesig (UIA-Timeouts).
/// </summary>
public class MapMarkerSelectionTests_Limit : BaseTest
{
    private const double Latitude = 52.52;
    private const double Longitude = 13.405;

    private static MapMarker Marker(string id, double latitude, double longitude)
    {
        var station = new StationListItem(id, id, null, string.Empty, [], false, false, string.Empty, string.Empty, latitude, longitude);
        return new MapMarker(station, latitude, longitude, PriceLevel.Normal, string.Empty, id);
    }

    private static List<MapMarker> Grid(int count)
    {
        var list = new List<MapMarker>();
        for (var i = 0; i < count; i++)
        {
            list.Add(Marker("s" + i, Latitude + ((i % 20) * 0.0005), Longitude + ((i / 20) * 0.0005)));
        }

        return list;
    }

    /// <summary>
    /// Prüft, dass bei 300 Stationen im Ausschnitt nur die Obergrenze aufgebaut wird, die Gesamtzahl im Ausschnitt aber korrekt bleibt.
    /// </summary>
    [Fact]
    public void Select_With300MarkersInView_RendersOnlyLimit()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, 800, 600);

        var selection = MapMarkerSelection.Select(Grid(300), viewport);

        Assert.Equal(300, selection.InViewport);
        Assert.Equal(MapMarkerSelection.MaxRendered, selection.Rendered.Count);
        Assert.True(selection.IsTruncated);
    }

    /// <summary>
    /// Prüft, dass die zur Bildmitte nächsten Stationen bevorzugt werden.
    /// </summary>
    [Fact]
    public void Select_OverLimit_PrefersMarkersNearestToCenter()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, 800, 600);
        var markers = Grid(300);
        markers.Add(Marker("center", Latitude, Longitude));

        var selection = MapMarkerSelection.Select(markers, viewport);

        Assert.Contains(selection.Rendered, marker => marker.Station.Id == "center");
    }

    /// <summary>
    /// Prüft, dass Stationen außerhalb des Ausschnitts weder gezählt noch aufgebaut werden.
    /// </summary>
    [Fact]
    public void Select_MarkersOutsideViewport_AreIgnored()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, 800, 600);
        var markers = new List<MapMarker> { Marker("in", Latitude, Longitude), Marker("far", 48.1, 11.5) };

        var selection = MapMarkerSelection.Select(markers, viewport);

        Assert.Equal(1, selection.InViewport);
        Assert.Equal(["in"], selection.Rendered.Select(marker => marker.Station.Id));
        Assert.False(selection.IsTruncated);
    }

    /// <summary>
    /// Prüft, dass unterhalb der Obergrenze alle Stationen im Ausschnitt aufgebaut werden.
    /// </summary>
    [Fact]
    public void Select_BelowLimit_RendersAllInView()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, 800, 600);

        var selection = MapMarkerSelection.Select(Grid(40), viewport);

        Assert.Equal(40, selection.InViewport);
        Assert.Equal(40, selection.Rendered.Count);
        Assert.False(selection.IsTruncated);
    }

    /// <summary>
    /// Prüft den Zählertext bei begrenzter Darstellung (mit Hinweis auf das Hineinzoomen).
    /// </summary>
    [Fact]
    public void FormatStationCountLimited_MentionsShownAndZoom()
    {
        Assert.Equal("120 von 300 Stationen sichtbar, 100 dargestellt – für alle hineinzoomen", Tankradar.MAUI.Resources.Texts.MapTexts.FormatStationCountLimited(120, 300, 100));
    }
}
