namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass iOS und MacCatalyst die Verwendungszwecke für Kamera und Fotomediathek (Belegfotos) deklarieren; ohne sie bricht die Systemabfrage ab.
/// </summary>
public class PlatformManifestTests_Photos : BaseTest
{
    private static string ReadInfoPlist(string platform)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Tankradar.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, "src", "Tankradar.MAUI", "Platforms", platform, "Info.plist"));
    }

    /// <summary>
    /// Prüft beide Verwendungszwecke samt deutschem Text.
    /// </summary>
    /// <param name="platform">Der Plattformordner.</param>
    [Theory]
    [InlineData("iOS")]
    [InlineData("MacCatalyst")]
    public void InfoPlist_ContainsCameraAndPhotoDescriptions(string platform)
    {
        var plist = ReadInfoPlist(platform);

        var cameraIndex = plist.IndexOf("<key>NSCameraUsageDescription</key>", StringComparison.Ordinal);
        var photosIndex = plist.IndexOf("<key>NSPhotoLibraryUsageDescription</key>", StringComparison.Ordinal);
        Assert.True(cameraIndex >= 0, "Der Schlüssel NSCameraUsageDescription fehlt.");
        Assert.True(photosIndex >= 0, "Der Schlüssel NSPhotoLibraryUsageDescription fehlt.");
        Assert.StartsWith("<key>NSCameraUsageDescription</key>", plist[cameraIndex..], StringComparison.Ordinal);
        Assert.Contains("<string>Foto eines Tankbelegs aufnehmen</string>", plist[cameraIndex..], StringComparison.Ordinal);
        Assert.Contains("<string>Foto eines Tankbelegs auswählen</string>", plist[photosIndex..], StringComparison.Ordinal);
    }
}
