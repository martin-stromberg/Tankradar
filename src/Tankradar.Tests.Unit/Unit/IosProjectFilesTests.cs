using System.Xml.Linq;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Info.plist und Privacy-Manifest der iOS-Variante per XML-Auswertung: nur benötigte Berechtigungen mit deutschen Begründungen,
/// Verschlüsselungsangabe, Geräteangaben und die deklarierten Required-Reason-APIs.
/// </summary>
public class IosProjectFilesTests : BaseTest
{
    private static Dictionary<string, XElement> ReadPlist(params string[] relativePath)
    {
        var dict = XDocument.Load(MauiSourceFiles.PathOf(relativePath)).Root?.Element("dict");
        Assert.NotNull(dict);
        var entries = new Dictionary<string, XElement>(StringComparer.Ordinal);
        foreach (var key in dict.Elements("key"))
        {
            entries[key.Value] = (XElement)key.NextNode!;
        }

        return entries;
    }

    private static Dictionary<string, XElement> InfoPlist()
    {
        return ReadPlist("Platforms", "iOS", "Info.plist");
    }

    private static Dictionary<string, XElement> Manifest()
    {
        return ReadPlist("Platforms", "iOS", "Resources", "PrivacyInfo.xcprivacy");
    }

    /// <summary>
    /// Prüft, dass genau die drei benötigten Berechtigungen (Kamera, Fotos, Standort bei Nutzung) beschrieben sind und keine weiteren.
    /// </summary>
    [Fact]
    public void Info_plist_DeclaresOnlyCameraPhotosAndLocationWhenInUse()
    {
        var usageKeys = InfoPlist().Keys.Where(k => k.StartsWith("NS", StringComparison.Ordinal) && k.EndsWith("UsageDescription", StringComparison.Ordinal)).Order().ToList();

        Assert.Equal(["NSCameraUsageDescription", "NSLocationWhenInUseUsageDescription", "NSPhotoLibraryUsageDescription"], usageKeys);
    }

    /// <summary>
    /// Prüft, dass der Standorttext die Tankstellensuche in der Nähe und den Startpunkt einer Route nennt.
    /// </summary>
    [Fact]
    public void Info_plist_LocationTextMentionsNearbyStationsAndRouteStart()
    {
        var text = InfoPlist()["NSLocationWhenInUseUsageDescription"].Value;

        Assert.Contains("Tankstellen", text, StringComparison.Ordinal);
        Assert.Contains("Nähe", text, StringComparison.Ordinal);
        Assert.Contains("Route", text, StringComparison.Ordinal);
        Assert.Contains("Startpunkt", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass Kamera und Fotos nur für Belegfotos begründet sind und alle Begründungen ausgefüllt sind.
    /// </summary>
    [Fact]
    public void Info_plist_CameraAndPhotoTextsAreLimitedToReceipts()
    {
        var plist = InfoPlist();

        Assert.Contains("Tankbeleg", plist["NSCameraUsageDescription"].Value, StringComparison.Ordinal);
        Assert.Contains("Tankbeleg", plist["NSPhotoLibraryUsageDescription"].Value, StringComparison.Ordinal);
        Assert.All(plist.Where(p => p.Key.EndsWith("UsageDescription", StringComparison.Ordinal)), p => Assert.False(string.IsNullOrWhiteSpace(p.Value.Value)));
    }

    /// <summary>
    /// Prüft die Export-Compliance-Angabe (keine genehmigungspflichtige Verschlüsselung) und die Geräteangaben.
    /// </summary>
    [Fact]
    public void Info_plist_DeclaresExemptEncryptionAndArm64()
    {
        var plist = InfoPlist();

        Assert.Equal("false", plist["ITSAppUsesNonExemptEncryption"].Name.LocalName);
        Assert.Contains(plist["UIRequiredDeviceCapabilities"].Elements("string"), s => s.Value == "arm64");
        Assert.Contains(plist["UIDeviceFamily"].Elements("integer"), i => i.Value == "1");
    }

    /// <summary>
    /// Prüft, dass das Manifest kein Tracking deklariert und nur den Standort als App-Funktion erfasst, ohne Verknüpfung mit der Person.
    /// </summary>
    [Fact]
    public void Privacy_manifest_DeclaresNoTrackingAndOnlyPreciseLocationForAppFunctionality()
    {
        var manifest = Manifest();

        Assert.Equal("false", manifest["NSPrivacyTracking"].Name.LocalName);
        var collected = Assert.Single(manifest["NSPrivacyCollectedDataTypes"].Elements("dict"));
        Assert.Contains(collected.Elements("string"), s => s.Value == "NSPrivacyCollectedDataTypePreciseLocation");
        Assert.Contains(collected.Descendants("string"), s => s.Value == "NSPrivacyCollectedDataTypePurposeAppFunctionality");
        Assert.Empty(collected.Elements("true"));
    }

    /// <summary>
    /// Prüft die deklarierten Required-Reason-APIs samt Gründen (Dateizeitstempel, Systemstartzeit, Speicherplatz, UserDefaults).
    /// </summary>
    /// <param name="category">Die API-Kategorie.</param>
    /// <param name="reason">Der erwartete Grund-Code.</param>
    [Theory]
    [InlineData("NSPrivacyAccessedAPICategoryFileTimestamp", "C617.1")]
    [InlineData("NSPrivacyAccessedAPICategoryFileTimestamp", "3B52.1")]
    [InlineData("NSPrivacyAccessedAPICategorySystemBootTime", "35F9.1")]
    [InlineData("NSPrivacyAccessedAPICategoryDiskSpace", "E174.1")]
    [InlineData("NSPrivacyAccessedAPICategoryUserDefaults", "CA92.1")]
    public void Privacy_manifest_DeclaresRequiredReasonApi(string category, string reason)
    {
        var apis = Manifest()["NSPrivacyAccessedAPITypes"].Elements("dict").ToList();
        var entry = apis.Single(a => a.Elements("string").Any(s => s.Value == category));

        Assert.Contains(entry.Element("array")!.Elements("string"), s => s.Value == reason);
    }
}
