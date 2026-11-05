using Tankradar.MAUI.Services;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, wann die App ihr Fenster im Testmodus außerhalb des Bildschirms startet: nur im Testmodus und nur auf ausdrücklichen Wunsch.
/// </summary>
public class TestWindowModeTests_Selection : BaseTest
{
    private static Func<string, string?> Env(params (string Name, string Value)[] values)
    {
        return name => values.Where(entry => entry.Name == name).Select(entry => entry.Value).FirstOrDefault();
    }

    /// <summary>
    /// Prüft die Namen und Werte der Konstanten.
    /// </summary>
    [Fact]
    public void Constants_HaveExpectedValues()
    {
        Assert.Equal("TANKATLAS_TEST_WINDOW", TestDataPaths.TestWindowEnvironmentVariable);
        Assert.Equal("offscreen", TestDataPaths.TestWindowOffscreen);
        Assert.Equal("foreground", TestDataPaths.TestWindowForeground);
    }

    /// <summary>
    /// Prüft, dass im Testmodus mit dem Wert offscreen (auch abweichende Groß-/Kleinschreibung, Leerraum) das Fenster ausgeblendet wird.
    /// </summary>
    /// <param name="value">Der Wert von TANKATLAS_TEST_WINDOW.</param>
    [Theory]
    [InlineData("offscreen")]
    [InlineData("OffScreen")]
    [InlineData(" offscreen ")]
    public void TestMode_WithOffscreen_HidesWindow(string value)
    {
        var env = Env(
            (TestDataPaths.TestDataPathEnvironmentVariable, "testdata"),
            (TestDataPaths.TestWindowEnvironmentVariable, value));

        Assert.True(TestWindowMode.ShouldHideWindow(env));
    }

    /// <summary>
    /// Prüft, dass im Testmodus ohne oder mit anderem Wert (Vordergrundbetrieb, Unbekanntes) das Fenster normal startet.
    /// </summary>
    /// <param name="value">Der Wert von TANKATLAS_TEST_WINDOW.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("foreground")]
    [InlineData("hidden")]
    public void TestMode_WithoutOffscreen_KeepsWindowVisible(string? value)
    {
        var env = value is null
            ? Env((TestDataPaths.TestDataPathEnvironmentVariable, "testdata"))
            : Env((TestDataPaths.TestDataPathEnvironmentVariable, "testdata"), (TestDataPaths.TestWindowEnvironmentVariable, value));

        Assert.False(TestWindowMode.ShouldHideWindow(env));
    }

    /// <summary>
    /// Prüft, dass außerhalb des Testmodus das Fenster nie ausgeblendet wird, auch wenn die Variable gesetzt ist.
    /// </summary>
    /// <param name="testDataPath">Der Wert von TANKATLAS_TEST_DATA_PATH.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void OutsideTestMode_NeverHidesWindow(string? testDataPath)
    {
        var env = testDataPath is null
            ? Env((TestDataPaths.TestWindowEnvironmentVariable, "offscreen"))
            : Env((TestDataPaths.TestDataPathEnvironmentVariable, testDataPath), (TestDataPaths.TestWindowEnvironmentVariable, "offscreen"));

        Assert.False(TestWindowMode.ShouldHideWindow(env));
    }

    /// <summary>
    /// Prüft, dass ein fehlender Umgebungsleser abgelehnt wird.
    /// </summary>
    [Fact]
    public void NullEnvironmentReader_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TestWindowMode.ShouldHideWindow(null!));
    }
}
