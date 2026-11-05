using Tankradar.MAUI.Services.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass im Testmodus nie die echte Fotoauswahl genutzt wird und die Testauswahl die vorgegebene Datei liefert.
/// </summary>
public class ReceiptPhotoPickerSelectorTests_TestMode : BaseTest
{
    private static Func<string, string?> Env(params (string Name, string Value)[] values)
    {
        return name => values.Where(entry => entry.Name == name).Select(entry => entry.Value).FirstOrDefault();
    }

    /// <summary>
    /// Prüft, dass im Testmodus die Testauswahl die Datei samt Endung liefert, keine Aufnahme anbietet und die Plattform nie erzeugt.
    /// </summary>
    [Fact]
    public async Task Create_TestMode_UsesTestPicker()
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        await File.WriteAllBytesAsync(file, [7, 8, 9]);
        try
        {
            var created = false;
            var picker = ReceiptPhotoPickerSelector.Create(
                Env((TestDataPaths.TestDataPathEnvironmentVariable, "testdata"), (TestDataPaths.TestReceiptPhotoEnvironmentVariable, file)),
                () =>
                {
                    created = true;
                    return new FakeReceiptPhotoPicker();
                });

            var photo = await picker.PickAsync();

            Assert.False(created);
            Assert.IsType<TestReceiptPhotoPicker>(picker);
            Assert.False(picker.IsCaptureSupported);
            Assert.Null(await picker.CaptureAsync());
            Assert.Equal(".png", photo!.Extension);
            await using var stream = await photo.OpenReadAsync();
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            Assert.Equal([7, 8, 9], copy.ToArray());
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>
    /// Prüft, dass außerhalb des Testmodus die Plattformauswahl erzeugt wird, auch wenn die Testdatei gesetzt ist.
    /// </summary>
    [Fact]
    public void Create_Production_UsesPlatformPicker()
    {
        var platform = new FakeReceiptPhotoPicker();

        var picker = ReceiptPhotoPickerSelector.Create(Env((TestDataPaths.TestReceiptPhotoEnvironmentVariable, "beleg.png")), () => platform);

        Assert.Same(platform, picker);
    }

    /// <summary>
    /// Prüft, dass die Testauswahl ohne (vorhandene) Datei als Abbruch gilt.
    /// </summary>
    [Fact]
    public async Task TestPicker_NoFile_ReturnsNull()
    {
        Assert.Null(await new TestReceiptPhotoPicker(null).PickAsync());
        Assert.Null(await new TestReceiptPhotoPicker(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".jpg")).PickAsync());
    }
}
