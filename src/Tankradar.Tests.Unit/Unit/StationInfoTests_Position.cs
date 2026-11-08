using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die abgeleitete Position der Tankstelle: gültige Koordinaten werden übernommen, ungültige und (0, 0) verworfen.
/// </summary>
public class StationInfoTests_Position : BaseTest
{
    /// <summary>
    /// Prüft, dass gültige Koordinaten eine Position liefern.
    /// </summary>
    [Fact]
    public void Position_ValidCoordinates_ReturnsPosition()
    {
        var station = new StationInfo { Id = "id", Name = "Alpha", Latitude = 52.52, Longitude = 13.405 };

        var position = station.Position;

        Assert.NotNull(position);
        Assert.Equal(52.52, position.Latitude);
        Assert.Equal(13.405, position.Longitude);
    }

    /// <summary>
    /// Prüft, dass (0, 0) als fehlende Angabe gilt und keine Position liefert.
    /// </summary>
    [Fact]
    public void Position_ZeroCoordinates_ReturnsNull()
    {
        var station = new StationInfo { Id = "id", Name = "Alpha", Latitude = 0, Longitude = 0 };

        Assert.Null(station.Position);
    }

    /// <summary>
    /// Prüft, dass Koordinaten außerhalb des Wertebereichs keine Position liefern.
    /// </summary>
    /// <param name="latitude">Der Breitengrad.</param>
    /// <param name="longitude">Der Längengrad.</param>
    [Theory]
    [InlineData(91, 10)]
    [InlineData(52, 181)]
    [InlineData(double.NaN, 10)]
    [InlineData(52, double.PositiveInfinity)]
    public void Position_OutOfRange_ReturnsNull(double latitude, double longitude)
    {
        var station = new StationInfo { Id = "id", Name = "Alpha", Latitude = latitude, Longitude = longitude };

        Assert.Null(station.Position);
    }
}
