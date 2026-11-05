using Tankradar.MAUI.Services.Map;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Doppelklick-Erkennung des <see cref="MapDoubleTapDetector"/> für die Windows-Maus: Zeitfenster, Abstand und Neubeginn nach einem Doppeltipp.
/// </summary>
public class MapDoubleTapDetectorTests_Timing : BaseTest
{
    /// <summary>
    /// Prüft, dass zwei Tipps innerhalb von 500 ms und 4 Pixeln einen Doppeltipp ergeben.
    /// </summary>
    [Fact]
    public void Register_SecondTapWithinWindowAndDistance_IsDoubleTap()
    {
        var detector = new MapDoubleTapDetector();

        Assert.False(detector.Register(100, 100, 1000));
        Assert.True(detector.Register(102, 103, 1500));
    }

    /// <summary>
    /// Prüft, dass ein zweiter Tipp nach dem Zeitfenster ein Einzeltipp ist.
    /// </summary>
    [Fact]
    public void Register_SecondTapAfterWindow_IsSingle()
    {
        var detector = new MapDoubleTapDetector();

        Assert.False(detector.Register(100, 100, 1000));
        Assert.False(detector.Register(100, 100, 1501));
    }

    /// <summary>
    /// Prüft, dass ein zweiter Tipp mehr als 4 Pixel entfernt ein Einzeltipp ist.
    /// </summary>
    [Fact]
    public void Register_SecondTapFarAway_IsSingle()
    {
        var detector = new MapDoubleTapDetector();

        Assert.False(detector.Register(100, 100, 1000));
        Assert.False(detector.Register(105, 100, 1100));
    }

    /// <summary>
    /// Prüft, dass ein zweiter Tipp mit früherem Zeitstempel (rückläufige Uhr) ein Einzeltipp ist.
    /// </summary>
    [Fact]
    public void Register_SecondTapWithEarlierTimestamp_IsSingle()
    {
        var detector = new MapDoubleTapDetector();

        Assert.False(detector.Register(100, 100, 1000));
        Assert.False(detector.Register(100, 100, 900));
    }

    /// <summary>
    /// Prüft, dass nach einem Doppeltipp der dritte schnelle Tipp eine neue Folge beginnt.
    /// </summary>
    [Fact]
    public void Register_ThirdTapAfterDouble_StartsNewSequence()
    {
        var detector = new MapDoubleTapDetector();

        Assert.False(detector.Register(100, 100, 1000));
        Assert.True(detector.Register(100, 100, 1100));
        Assert.False(detector.Register(100, 100, 1200));
        Assert.True(detector.Register(100, 100, 1300));
    }
}
