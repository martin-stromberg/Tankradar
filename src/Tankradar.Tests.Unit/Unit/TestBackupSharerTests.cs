using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Testweitergabe ohne Systemdialog.
/// </summary>
public class TestBackupSharerTests : BaseTest
{
    /// <summary>
    /// Prüft, dass das Zielverzeichnis angelegt und eine vorhandene Kopie überschrieben wird.
    /// </summary>
    [Fact]
    public async Task ShareAsync_CopiesAndOverwrites()
    {
        using var temp = new TempDirectory();
        var source = temp.Combine("a.zip");
        var target = temp.Combine("ziel", "x");
        var sharer = new TestBackupSharer(target);
        await File.WriteAllBytesAsync(source, [1]);
        await sharer.ShareAsync(source, "t");
        await File.WriteAllBytesAsync(source, [2, 2]);

        await sharer.ShareAsync(source, "t");

        Assert.Equal([2, 2], await File.ReadAllBytesAsync(Path.Combine(target, "a.zip")));
    }

    /// <summary>
    /// Prüft, dass eine fehlende Quelle eine Ausnahme auslöst.
    /// </summary>
    [Fact]
    public async Task ShareAsync_MissingSource_Throws()
    {
        using var temp = new TempDirectory();

        await Assert.ThrowsAsync<FileNotFoundException>(() => new TestBackupSharer(temp.Combine("ziel")).ShareAsync(temp.Combine("fehlt.zip"), "t"));
    }
}
