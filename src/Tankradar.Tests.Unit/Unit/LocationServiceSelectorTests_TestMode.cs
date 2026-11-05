using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Location;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass im Testmodus nie die echte Plattform-Standortabfrage genutzt wird und der Teststandort nur über die neuen Variablennamen wirkt.
/// </summary>
public class LocationServiceSelectorTests_TestMode : BaseTest
{
    private static Func<string, string?> Env(params (string Name, string Value)[] values)
    {
        return name => values.Where(entry => entry.Name == name).Select(entry => entry.Value).FirstOrDefault();
    }

    private static (ILocationService Service, bool PlatformCreated) Create(Func<string, string?> env)
    {
        var created = false;
        var service = LocationServiceSelector.Create(env, () =>
        {
            created = true;
            return new TestLocationService(null);
        });
        return (service, created);
    }

    /// <summary>
    /// Prüft, dass im Testmodus mit gültigem Standort der feste Standort geliefert und die Plattform nie erzeugt wird.
    /// </summary>
    [Fact]
    public async Task TestMode_WithValidLocation_UsesFixedPositionAndNeverCreatesPlatformService()
    {
        var (service, created) = Create(Env(
            (TestDataPaths.TestDataPathEnvironmentVariable, "testdata"),
            (TestDataPaths.TestLocationEnvironmentVariable, "52.5200,13.4050")));

        var result = await service.GetCurrentLocationAsync(GpsUsage.WhileInUse);

        Assert.False(created);
        Assert.IsType<TestLocationService>(service);
        Assert.Equal(LocationStatus.Available, result.Status);
        Assert.Equal(52.52, result.Position!.Latitude);
        Assert.Equal(13.405, result.Position.Longitude);
    }

    /// <summary>
    /// Prüft, dass im Testmodus ohne oder mit ungültigem Standort „nicht verfügbar“ gemeldet und die Plattform nie erzeugt wird.
    /// </summary>
    /// <param name="location">Der Wert von TANKATLAS_TEST_LOCATION.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("52.5")]
    [InlineData("52.5,13.4,1")]
    [InlineData("91,0")]
    [InlineData("0,181")]
    [InlineData("NaN,0")]
    [InlineData("52,5200,13,4050")]
    public async Task TestMode_WithInvalidLocation_ReportsUnavailable(string? location)
    {
        var (service, created) = Create(Env(
            (TestDataPaths.TestDataPathEnvironmentVariable, "testdata"),
            (TestDataPaths.TestLocationEnvironmentVariable, location!)));

        var result = await service.GetCurrentLocationAsync(GpsUsage.Always);

        Assert.False(created);
        Assert.Equal(LocationStatus.Unavailable, result.Status);
    }

    /// <summary>
    /// Prüft, dass außerhalb des Testmodus der Plattformdienst gewählt wird, auch wenn ein Teststandort gesetzt ist.
    /// </summary>
    [Fact]
    public void NormalMode_UsesPlatformService_EvenWithTestLocation()
    {
        var (_, created) = Create(Env((TestDataPaths.TestLocationEnvironmentVariable, "52.52,13.405")));

        Assert.True(created);
    }

    /// <summary>
    /// Prüft, dass der alte Variablenname den Testmodus nicht aktiviert.
    /// </summary>
    [Fact]
    public void OldVariableName_DoesNotActivateTestMode()
    {
        var (_, created) = Create(Env(("TEST_DATA_PATH", "testdata"), (TestDataPaths.TestLocationEnvironmentVariable, "52.52,13.405")));

        Assert.True(created);
    }

    /// <summary>
    /// Prüft das Lesen des Teststandorts: Dezimalpunkt, Leerzeichen und Grenzwerte.
    /// </summary>
    [Fact]
    public void TryParseLocation_AcceptsInvariantFormat()
    {
        Assert.True(LocationServiceSelector.TryParseLocation(" 52.52 , 13.405 ", out var position));
        Assert.Equal(52.52, position!.Latitude);
        Assert.True(LocationServiceSelector.TryParseLocation("-90,180", out _));
        Assert.False(LocationServiceSelector.TryParseLocation(null, out var none));
        Assert.Null(none);
    }
}
