using System.Text;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Erkennung des Bildformats an der Signatur durch <see cref="ReceiptPhotoStore.DetectExtension"/>.
/// </summary>
public class ReceiptPhotoStoreTests_Detection : BaseTest
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "tankatlas-detect-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Legt das temporäre Verzeichnis an.
    /// </summary>
    public ReceiptPhotoStoreTests_Detection()
    {
        Directory.CreateDirectory(_directory);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

        base.Dispose(disposing);
    }

    private string Write(byte[] content)
    {
        var path = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".bin");
        File.WriteAllBytes(path, content);
        return path;
    }

    private static byte[] Ftyp(string brand)
    {
        return [0, 0, 0, 0x18, .. Encoding.ASCII.GetBytes("ftyp"), .. Encoding.ASCII.GetBytes(brand), 0, 0];
    }

    /// <summary>
    /// Prüft, dass die Marke „mif1“ als HEIF erkannt wird.
    /// </summary>
    [Fact]
    public void DetectExtension_Mif1_ReturnsHeif()
    {
        Assert.Equal(".heif", ReceiptPhotoStore.DetectExtension(Write(Ftyp("mif1"))));
    }

    /// <summary>
    /// Prüft, dass die Marke „msf1“ als HEIF erkannt wird.
    /// </summary>
    [Fact]
    public void DetectExtension_Msf1_ReturnsHeif()
    {
        Assert.Equal(".heif", ReceiptPhotoStore.DetectExtension(Write(Ftyp("msf1"))));
    }

    /// <summary>
    /// Prüft, dass eine unbekannte Marke abgelehnt wird.
    /// </summary>
    [Fact]
    public void DetectExtension_UnknownFtypBrand_ReturnsNull()
    {
        Assert.Null(ReceiptPhotoStore.DetectExtension(Write(Ftyp("mp42"))));
    }

    /// <summary>
    /// Prüft, dass eine zu kurze Datei abgelehnt wird.
    /// </summary>
    [Fact]
    public void DetectExtension_TooShortFile_ReturnsNull()
    {
        Assert.Null(ReceiptPhotoStore.DetectExtension(Write([0, 0, 0, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p'])));
        Assert.Null(ReceiptPhotoStore.DetectExtension(Write([])));
    }

    /// <summary>
    /// Prüft, dass die Marke „heic“ als HEIC erkannt wird.
    /// </summary>
    [Fact]
    public void DetectExtension_Heic_ReturnsHeic()
    {
        Assert.Equal(".heic", ReceiptPhotoStore.DetectExtension(Write(Ftyp("heic"))));
    }
}
