using System.Text.Json;

namespace Tankradar.MAUI.Services;

/// <summary>
/// Verwaltet die Konfiguration der Tankradar-App, insbesondere die App-Kennung (Bundle-ID).
/// </summary>
public class AppConfiguration
{
    /// <summary>
    /// Standardwert der Bundle-ID (keine Geheiminformation); per Umgebungsvariable oder appsettings.json überschreibbar.
    /// </summary>
    public const string DefaultBundleId = "de.martinstromberg.tankradar";

    private const string BundleIdEnvironmentVariable = "TANKRADAR_BUNDLE_ID";
    private const string SettingsFileName = "appsettings.json";

    /// <summary>
    /// Erstellt eine neue <see cref="AppConfiguration"/> und ermittelt die Bundle-ID zunächst aus der Umgebungsvariable oder dem Standardwert.
    /// </summary>
    public AppConfiguration()
    {
        BundleId = Environment.GetEnvironmentVariable(BundleIdEnvironmentVariable) is { Length: > 0 } envBundleId
            ? envBundleId
            : DefaultBundleId;
    }

    /// <summary>
    /// Die App-Kennung (Bundle-ID) für iOS und eine spätere Android-Auslieferung.
    /// </summary>
    public string BundleId { get; private set; }

    /// <summary>
    /// Lädt die Bundle-ID aus der gebündelten <c>appsettings.json</c> nach, sofern vorhanden und keine Umgebungsvariable gesetzt ist.
    /// </summary>
    public async Task LoadFromSettingsFileAsync()
    {
        if (Environment.GetEnvironmentVariable(BundleIdEnvironmentVariable) is { Length: > 0 })
        {
            return;
        }

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(SettingsFileName).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

            if (document.RootElement.TryGetProperty("AppConfiguration", out var appConfigurationElement)
                && appConfigurationElement.TryGetProperty("BundleId", out var bundleIdElement)
                && bundleIdElement.GetString() is { Length: > 0 } bundleId)
            {
                BundleId = bundleId;
            }
        }
        catch (FileNotFoundException)
        {
        }
        catch (JsonException)
        {
        }
        catch (IOException)
        {
        }
    }
}
