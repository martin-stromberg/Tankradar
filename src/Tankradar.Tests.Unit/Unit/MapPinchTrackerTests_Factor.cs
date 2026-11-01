using Tankradar.MAUI.Services.Map;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Auswertung der Kneifgeste im <see cref="MapPinchTracker"/>: Kumulieren der Einzelfaktoren, Gestenmittelpunkt und Zurücksetzen.
/// </summary>
public class MapPinchTrackerTests_Factor : BaseTest
{
    /// <summary>
    /// Prüft, dass die Einzelfaktoren seit dem letzten Update zum Gesamtfaktor multipliziert werden.
    /// </summary>
    [Fact]
    public void Update_MultipliesIncrementalFactors()
    {
        var tracker = new MapPinchTracker();
        tracker.Start(100, 100);

        tracker.Update(0.9, 100, 100);
        tracker.Update(1.1, 100, 100);
        tracker.Update(1.2, 100, 100);

        Assert.True(tracker.IsActive);
        Assert.Equal(1.188, tracker.Factor, 9);
    }

    /// <summary>
    /// Prüft, dass ein Update ohne laufende Geste wirkungslos bleibt.
    /// </summary>
    [Fact]
    public void Update_BeforeStart_IsIgnored()
    {
        var tracker = new MapPinchTracker();

        tracker.Update(2, 100, 100);

        Assert.False(tracker.IsActive);
        Assert.Equal(1, tracker.Factor);
    }

    /// <summary>
    /// Prüft, dass ungültige Einzelfaktoren nicht in den Gesamtfaktor eingehen.
    /// </summary>
    /// <param name="scale">Der ungültige Einzelfaktor.</param>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.5)]
    [InlineData(double.NaN)]
    public void Update_InvalidScale_IsIgnored(double scale)
    {
        var tracker = new MapPinchTracker();
        tracker.Start(0, 0);
        tracker.Update(1.5, 0, 0);

        tracker.Update(scale, 0, 0);

        Assert.Equal(1.5, tracker.Factor, 9);
    }

    /// <summary>
    /// Prüft, dass der Gestenmittelpunkt dem zuletzt gemeldeten entspricht.
    /// </summary>
    [Fact]
    public void Origin_IsLastReportedOrigin()
    {
        var tracker = new MapPinchTracker();
        tracker.Start(10, 20);

        tracker.Update(1.1, 30, 40);
        tracker.Update(1.1, 50, 60);

        Assert.Equal(50, tracker.OriginX);
        Assert.Equal(60, tracker.OriginY);
    }

    /// <summary>
    /// Prüft, dass ein erneuter Start während einer laufenden Geste Faktor und Mittelpunkt zurücksetzt.
    /// </summary>
    [Fact]
    public void Start_WhileActive_ResetsFactorAndOrigin()
    {
        var tracker = new MapPinchTracker();
        tracker.Start(10, 20);
        tracker.Update(2, 30, 40);

        tracker.Start(50, 60);

        Assert.True(tracker.IsActive);
        Assert.Equal(1, tracker.Factor);
        Assert.Equal(50, tracker.OriginX);
        Assert.Equal(60, tracker.OriginY);
    }

    /// <summary>
    /// Prüft, dass das Beenden die Geste deaktiviert und den Faktor zurücksetzt.
    /// </summary>
    [Fact]
    public void End_ResetsAndDeactivates()
    {
        var tracker = new MapPinchTracker();
        tracker.Start(0, 0);
        tracker.Update(2, 0, 0);

        tracker.End();

        Assert.False(tracker.IsActive);
        Assert.Equal(1, tracker.Factor);
    }
}
