using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das stille Aufräumen der Sicherung und den Auswahltitel aus den zentralen Texten.
/// </summary>
public class BackupFileCleanupTests : BaseTest
{
    private readonly TempDirectory _temp = new();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _temp.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Prüft, dass Dateien und Verzeichnisse gelöscht und fehlende oder leere Angaben ignoriert werden.
    /// </summary>
    [Fact]
    public void Delete_RemovesAndIgnoresMissing()
    {
        var file = _temp.Combine("a.txt");
        File.WriteAllText(file, "x");
        var directory = _temp.Combine("d");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "b.txt"), "y");

        BackupFileCleanup.DeleteFile(file);
        BackupFileCleanup.DeleteFile(null);
        BackupFileCleanup.DeleteFile(_temp.Combine("fehlt.txt"));
        BackupFileCleanup.DeleteDirectory(directory, onlyIfEmpty: true);
        var keptNonEmpty = Directory.Exists(directory);
        BackupFileCleanup.DeleteDirectory(directory);
        BackupFileCleanup.DeleteDirectory(null);

        Assert.False(File.Exists(file));
        Assert.True(keptNonEmpty);
        Assert.False(Directory.Exists(directory));
    }

    /// <summary>
    /// Prüft, dass eine gesperrte Datei beim stillen Löschen keine Ausnahme auslöst.
    /// </summary>
    [Fact]
    public void DeleteFile_Locked_DoesNotThrow()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var file = _temp.Combine("gesperrt.txt");
        File.WriteAllText(file, "x");
        using var held = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);

        var exception = Record.Exception(() => BackupFileCleanup.DeleteFile(file));

        Assert.Null(exception);
    }

    /// <summary>
    /// Prüft, dass der Auswahltitel als zentraler Text vorhanden ist.
    /// </summary>
    [Fact]
    public void PickerTitle_IsCentralText()
    {
        Assert.Equal("Sicherung auswählen", BackupTexts.PickerTitle);
    }
}
