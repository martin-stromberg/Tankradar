namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die Plattformdateien den Standortzugriff deklarieren (Zweck im Berechtigungsdialog, Capability, Datenschutzangabe).
/// </summary>
public class PlatformManifestTests_Location : BaseTest
{
    private const string PurposeKey = "<key>NSLocationWhenInUseUsageDescription</key>";
    private const string PurposeText = "<string>Ermittlung von Tankstellen in der Nähe</string>";

    private static string ReadPlatformFile(params string[] relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Tankradar.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var path = Path.Combine([directory.FullName, "src", "Tankradar.MAUI", "Platforms", .. relativePath]);
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Prüft, dass iOS und MacCatalyst den Verwendungszweck für den Standort „bei Nutzung“ enthalten.
    /// </summary>
    /// <param name="platform">Der Plattformordner.</param>
    [Theory]
    [InlineData("iOS")]
    [InlineData("MacCatalyst")]
    public void InfoPlist_ContainsWhenInUseUsageDescription(string platform)
    {
        var plist = ReadPlatformFile(platform, "Info.plist");

        var keyIndex = plist.IndexOf(PurposeKey, StringComparison.Ordinal);
        Assert.True(keyIndex >= 0, "Der Schlüssel NSLocationWhenInUseUsageDescription fehlt.");
        Assert.Contains(PurposeText, plist[keyIndex..], StringComparison.Ordinal);
        Assert.DoesNotContain("NSLocationAlways", plist, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass das Windows-Manifest die Capability „location“ enthält.
    /// </summary>
    [Fact]
    public void AppxManifest_ContainsLocationCapability()
    {
        var manifest = ReadPlatformFile("Windows", "Package.appxmanifest");

        Assert.Contains("<DeviceCapability Name=\"location\" />", manifest, StringComparison.Ordinal);
        Assert.Contains("runFullTrust", manifest, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass das iOS-Datenschutzmanifest die Standortnutzung ohne Tracking angibt.
    /// </summary>
    [Fact]
    public void PrivacyManifest_DeclaresPreciseLocationWithoutTracking()
    {
        var manifest = ReadPlatformFile("iOS", "Resources", "PrivacyInfo.xcprivacy");

        Assert.Contains("NSPrivacyCollectedDataTypePreciseLocation", manifest, StringComparison.Ordinal);
        Assert.Contains("NSPrivacyCollectedDataTypePurposeAppFunctionality", manifest, StringComparison.Ordinal);
        Assert.Contains("<key>NSPrivacyTracking</key>", manifest, StringComparison.Ordinal);
    }
}
