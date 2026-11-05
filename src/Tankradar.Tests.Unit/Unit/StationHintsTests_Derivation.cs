using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die aus den Quelldaten abgeleiteten Hinweise „Automatentankstelle“ und „Preis unbestätigt“.
/// </summary>
public class StationHintsTests_Derivation : BaseTest
{
    private static readonly DateTime Now = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Prüft, dass die ausdrückliche Angabe „durchgehend geöffnet“ maßgeblich ist.
    /// </summary>
    [Fact]
    public void IsAutomatedStation_ExplicitWholeDay_IsUsed()
    {
        Assert.True(StationHints.IsAutomatedStation(true, []));
        Assert.False(StationHints.IsAutomatedStation(false, [new OpeningTimeEntry("Mo-So", "00:00:00", "24:00:00")]));
    }

    /// <summary>
    /// Prüft die Ableitung aus Öffnungszeiten, die rund um die Uhr gelten.
    /// </summary>
    [Fact]
    public void IsAutomatedStation_AroundTheClockOpeningTimes_IsTrue()
    {
        var times = new[]
        {
            new OpeningTimeEntry("Mo-Sa", "00:00:00", "24:00:00"),
            new OpeningTimeEntry("So", "00:00:00", "23:59:59"),
        };

        Assert.True(StationHints.IsAutomatedStation(null, times));
    }

    /// <summary>
    /// Prüft, dass unbekannte oder eingeschränkte Öffnungszeiten keinen Hinweis erzeugen.
    /// </summary>
    [Fact]
    public void IsAutomatedStation_UnknownOrLimitedTimes_IsFalse()
    {
        Assert.False(StationHints.IsAutomatedStation(null, []));
        Assert.False(StationHints.IsAutomatedStation(null, [new OpeningTimeEntry("Mo-Fr", "06:00:00", "22:00:00")]));
        Assert.False(StationHints.IsAutomatedStation(null, [new OpeningTimeEntry("Mo-So", "00:00:00", "24:00:00"), new OpeningTimeEntry("Feiertag", "08:00:00", "20:00:00")]));
    }

    /// <summary>
    /// Prüft, dass „Preis unbestätigt“ ab einem veralteten Preis (60 Minuten) gilt.
    /// </summary>
    [Fact]
    public void HasUnconfirmedPrice_FollowsStaleBoundary()
    {
        var fresh = new FuelPrice(FuelType.Diesel, 1.7m, Now.AddMinutes(-59));
        var stale = new FuelPrice(FuelType.SuperE5, 1.8m, Now.AddMinutes(-60));

        Assert.False(StationHints.HasUnconfirmedPrice([fresh], Now));
        Assert.True(StationHints.HasUnconfirmedPrice([fresh, stale], Now));
        Assert.False(StationHints.HasUnconfirmedPrice([], Now));
    }
}
