using Tankradar.MAUI.Services;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Ermittlung der Bundle-ID durch <see cref="AppConfiguration"/> aus Umgebungsvariable und Standardwert.
/// </summary>
public class AppConfigurationTests_BundleId : BaseTest
{
    private const string BundleIdEnvironmentVariable = "TANKRADAR_BUNDLE_ID";

    private readonly string? _originalValue = Environment.GetEnvironmentVariable(BundleIdEnvironmentVariable);

    /// <summary>
    /// Prüft, dass der Anzeigename „Tankatlas“ lautet, die Bundle-ID aber unverändert bleibt.
    /// </summary>
    [Fact]
    public void AppDisplayName_IsTankatlas_WhileBundleIdKeepsTechnicalName()
    {
        Assert.Equal("Tankatlas", AppConfiguration.AppDisplayName);
        Assert.Equal("de.martinstromberg.tankradar", AppConfiguration.DefaultBundleId);
    }

    /// <summary>
    /// Prüft, dass ohne Umgebungsvariable der Standardwert verwendet wird.
    /// </summary>
    [Fact]
    public void Constructor_WithoutEnvironmentVariable_UsesDefaultBundleId()
    {
        Environment.SetEnvironmentVariable(BundleIdEnvironmentVariable, null);

        var configuration = new AppConfiguration();

        Assert.Equal(AppConfiguration.DefaultBundleId, configuration.BundleId);
    }

    /// <summary>
    /// Prüft, dass eine gesetzte Umgebungsvariable den Standardwert überschreibt.
    /// </summary>
    [Fact]
    public void Constructor_WithEnvironmentVariable_UsesEnvironmentValue()
    {
        Environment.SetEnvironmentVariable(BundleIdEnvironmentVariable, "com.example.tankradar");

        var configuration = new AppConfiguration();

        Assert.Equal("com.example.tankradar", configuration.BundleId);
    }

    /// <summary>
    /// Prüft, dass das Nachladen der Einstellungsdatei eine per Umgebungsvariable gesetzte Bundle-ID nicht überschreibt.
    /// </summary>
    /// <returns>Eine Aufgabe, die den asynchronen Testlauf repräsentiert.</returns>
    [Fact]
    public async Task LoadFromSettingsFileAsync_WithEnvironmentVariable_KeepsEnvironmentValue()
    {
        Environment.SetEnvironmentVariable(BundleIdEnvironmentVariable, "com.example.tankradar");
        var configuration = new AppConfiguration();

        await configuration.LoadFromSettingsFileAsync();

        Assert.Equal("com.example.tankradar", configuration.BundleId);
    }

    /// <summary>
    /// Stellt die ursprüngliche Umgebungsvariable wieder her.
    /// </summary>
    /// <param name="disposing"><see langword="true"/>, wenn der Aufruf von Dispose stammt.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Environment.SetEnvironmentVariable(BundleIdEnvironmentVariable, _originalValue);
        }

        base.Dispose(disposing);
    }
}
