namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Verwendungszwecke für Kamera und Fotomediathek (Belegfotos) in den Info.plist von iOS und MacCatalyst; ohne den Kamera-Zweck bricht die Systemabfrage ab.
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
    /// Prüft, dass iOS nur die Kamera begründet: Die Fotoauswahl des Systems (PHPicker) und die Aufnahme brauchen ab .NET MAUI 10.0.60 keine Fotos-Berechtigung (ADR 0016).
    /// </summary>
    [Fact]
    public void InfoPlist_iOS_ContainsOnlyCameraDescription()
    {
        var plist = ReadInfoPlist("iOS");

        Assert.Contains("<key>NSCameraUsageDescription</key>", plist, StringComparison.Ordinal);
        Assert.Contains("<string>Foto eines Tankbelegs aufnehmen</string>", plist, StringComparison.Ordinal);
        Assert.DoesNotContain("NSPhotoLibraryUsageDescription", plist, StringComparison.Ordinal);
        Assert.DoesNotContain("NSPhotoLibraryAddUsageDescription", plist, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass MacCatalyst weiterhin Kamera und Fotomediathek samt deutschem Text deklariert (nicht Teil der iOS-Auslieferung, nicht geräteerprobt).
    /// </summary>
    [Fact]
    public void InfoPlist_MacCatalyst_ContainsCameraAndPhotoDescriptions()
    {
        var plist = ReadInfoPlist("MacCatalyst");

        var cameraIndex = plist.IndexOf("<key>NSCameraUsageDescription</key>", StringComparison.Ordinal);
        var photosIndex = plist.IndexOf("<key>NSPhotoLibraryUsageDescription</key>", StringComparison.Ordinal);
        Assert.True(cameraIndex >= 0, "Der Schlüssel NSCameraUsageDescription fehlt.");
        Assert.True(photosIndex >= 0, "Der Schlüssel NSPhotoLibraryUsageDescription fehlt.");
        Assert.Contains("<string>Foto eines Tankbelegs aufnehmen</string>", plist[cameraIndex..], StringComparison.Ordinal);
        Assert.Contains("<string>Foto eines Tankbelegs auswählen</string>", plist[photosIndex..], StringComparison.Ordinal);
    }
}
