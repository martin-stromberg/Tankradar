using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass im Testmodus nie die echte Dateiauswahl genutzt wird und die Testauswahl die vorgegebene Datei liefert.
/// </summary>
public class BackupFilePickerSelectorTests_TestMode : BaseTest
{
    private static Func<string, string?> Env(params (string Name, string Value)[] values)
    {
        return name => values.Where(entry => entry.Name == name).Select(entry => entry.Value).FirstOrDefault();
    }

    /// <summary>
    /// Prüft, dass im Testmodus die Testauswahl die Datei liefert und die Plattform nie erzeugt wird.
    /// </summary>
    [Fact]
    public async Task Create_TestMode_UsesTestPicker()
    {
        using var temp = new TempDirectory();
        var file = temp.Combine("sicherung.zip");
        await File.WriteAllBytesAsync(file, [7, 8, 9]);
        var created = false;

        var picker = BackupFilePickerSelector.Create(
            Env((TestDataPaths.TestDataPathEnvironmentVariable, "testdata"), (TestDataPaths.TestBackupImportEnvironmentVariable, file)),
            () =>
            {
                created = true;
                return new FakeBackupFilePicker();
            });
        var picked = await picker.PickAsync();

        Assert.False(created);
        Assert.IsType<TestBackupFilePicker>(picker);
        Assert.Equal("sicherung.zip", picked!.FileName);
        await using var stream = await picked.OpenReadAsync();
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        Assert.Equal([7, 8, 9], copy.ToArray());
    }

    /// <summary>
    /// Prüft, dass außerhalb des Testmodus die Plattformauswahl erzeugt wird.
    /// </summary>
    [Fact]
    public void Create_Production_UsesPlatformPicker()
    {
        var platform = new FakeBackupFilePicker();

        Assert.Same(platform, BackupFilePickerSelector.Create(Env((TestDataPaths.TestBackupImportEnvironmentVariable, "x.zip")), () => platform));
    }

    /// <summary>
    /// Prüft, dass die Testauswahl ohne (vorhandene) Datei als Abbruch gilt.
    /// </summary>
    [Fact]
    public async Task TestPicker_NoFile_ReturnsNull()
    {
        Assert.Null(await new TestBackupFilePicker(null).PickAsync());
        Assert.Null(await new TestBackupFilePicker("  ").PickAsync());
        Assert.Null(await new TestBackupFilePicker(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip")).PickAsync());
    }

    /// <summary>
    /// Prüft, dass im Testmodus ohne Importvariable die Auswahl als abgebrochen gilt.
    /// </summary>
    [Fact]
    public async Task Create_TestModeWithoutImportFile_IsCancelled()
    {
        var picker = BackupFilePickerSelector.Create(Env((TestDataPaths.TestDataPathEnvironmentVariable, "testdata")), () => new FakeBackupFilePicker());

        Assert.Null(await picker.PickAsync());
    }
}
