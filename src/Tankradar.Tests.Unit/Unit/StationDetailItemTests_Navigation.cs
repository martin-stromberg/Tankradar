using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die Detailangaben die Position der Tankstelle als Navigationsziel führen.
/// </summary>
public class StationDetailItemTests_Navigation : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    /// <summary>
    /// Prüft, dass <see cref="StationDetailItem.HasPosition"/> aus dem Vorliegen einer Position folgt.
    /// </summary>
    [Fact]
    public void HasPosition_ReflectsPosition()
    {
        var withPosition = Item(position: new GeoPosition(52.52, 13.405));
        var withoutPosition = Item(position: null);

        Assert.True(withPosition.HasPosition);
        Assert.False(withoutPosition.HasPosition);
    }

    /// <summary>
    /// Prüft, dass der Detail-Builder gültige Koordinaten der Tankstelle in die Detailangaben übernimmt.
    /// </summary>
    [Fact]
    public void Build_ValidCoordinates_SetsPosition()
    {
        var station = new StationInfo
        {
            Id = "id",
            Name = "Alpha",
            Latitude = 52.52,
            Longitude = 13.405,
        };

        var item = StationDetailBuilder.Build(station, SearchTestData.AllFuels, null, Now);

        Assert.Equal(52.52, item.Position?.Latitude);
        Assert.Equal(13.405, item.Position?.Longitude);
        Assert.True(item.HasPosition);
    }

    /// <summary>
    /// Prüft, dass fehlende Koordinaten (Standardwert 0, 0) keine Position liefern.
    /// </summary>
    [Fact]
    public void Build_ZeroCoordinates_LeavesPositionNull()
    {
        var station = new StationInfo { Id = "id", Name = "Alpha" };

        var item = StationDetailBuilder.Build(station, SearchTestData.AllFuels, null, Now);

        Assert.Null(item.Position);
        Assert.False(item.HasPosition);
    }

    private static StationDetailItem Item(GeoPosition? position)
    {
        return new StationDetailItem(
            "id",
            "Alpha",
            "",
            "",
            "",
            "",
            [],
            false,
            false,
            [],
            "",
            Position: position);
    }
}
