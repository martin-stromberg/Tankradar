using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft <see cref="ReceiptPhotoStore"/> auf einem temporären Datenverzeichnis: Übernahme mit Dateischutz, Endungen, Größe, Löschen und Pfadschutz.
/// </summary>
public class ReceiptPhotoStoreTests_Files : BaseTest
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "tankatlas-receipts-" + Guid.NewGuid().ToString("N"));
    private readonly FakeDatabaseFileProtector _protector = new();
    private readonly RecordingLogger<ReceiptPhotoStore> _logger = new();
    private readonly ReceiptPhotoStore _store;

    /// <summary>
    /// Legt das temporäre Datenverzeichnis und die Ablage an.
    /// </summary>
    public ReceiptPhotoStoreTests_Files()
    {
        Directory.CreateDirectory(_directory);
        _store = new ReceiptPhotoStore(new FakeAppDataPathProvider(_directory), _protector, _logger);
    }

    private string ReceiptDirectory
    {
        get { return Path.Combine(_directory, ReceiptPhotoStore.DirectoryName); }
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

    /// <summary>
    /// Prüft, dass ein unterstütztes Foto unverändert unter einem neuen Namen abgelegt und geschützt wird.
    /// </summary>
    [Fact]
    public async Task Import_SupportedPhoto_CopiesAndProtects()
    {
        byte[] content = [1, 2, 3, 4];

        var result = await _store.ImportAsync(new MemoryStream(content), ".JPG");

        Assert.Equal(ReceiptPhotoError.None, result.Error);
        Assert.Matches("^[0-9a-f]{32}\\.jpg$", result.FileName);
        var path = Path.Combine(ReceiptDirectory, result.FileName!);
        Assert.Equal(content, await File.ReadAllBytesAsync(path));
        Assert.Equal([path], _protector.ProtectedFiles);
        Assert.True(_store.Exists(result.FileName!));
        Assert.Equal(path, _store.GetFullPath(result.FileName!));
    }

    /// <summary>
    /// Prüft, dass eine nicht unterstützte Endung ohne Datei abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task Import_UnsupportedExtension_IsRejected()
    {
        var result = await _store.ImportAsync(new MemoryStream([1, 2, 3]), ".gif");

        Assert.Equal(new ReceiptPhotoImportResult(null, ReceiptPhotoError.UnsupportedFormat), result);
        Assert.False(Directory.Exists(ReceiptDirectory) && Directory.EnumerateFiles(ReceiptDirectory).Any());
    }

    /// <summary>
    /// Prüft, dass ein Foto über 20 MB abgelehnt wird und keine Datei zurückbleibt.
    /// </summary>
    [Fact]
    public async Task Import_TooLarge_IsRejectedAndLeavesNoFile()
    {
        var content = new byte[TankbookEntryLimits.MaxReceiptPhotoBytes + 1];

        var result = await _store.ImportAsync(new MemoryStream(content), ".png");

        Assert.Equal(new ReceiptPhotoImportResult(null, ReceiptPhotoError.TooLarge), result);
        Assert.Empty(Directory.EnumerateFiles(ReceiptDirectory));
        Assert.Empty(_protector.ProtectedFiles);
    }

    /// <summary>
    /// Prüft, dass Löschen die Datei entfernt und ein zweites Löschen nichts tut.
    /// </summary>
    [Fact]
    public async Task Delete_RemovesFile()
    {
        var fileName = (await _store.ImportAsync(new MemoryStream([1]), ".heic")).FileName!;

        _store.Delete(fileName);
        _store.Delete(fileName);

        Assert.False(_store.Exists(fileName));
        Assert.Empty(Directory.EnumerateFiles(ReceiptDirectory));
    }

    /// <summary>
    /// Prüft, dass ungültige Dateinamen (Pfadangaben) ignoriert werden und keine Datei außerhalb der Ablage gelöscht wird.
    /// </summary>
    [Fact]
    public void Delete_InvalidFileName_IsIgnored()
    {
        var outside = Path.Combine(_directory, "tankatlas.db");
        File.WriteAllText(outside, "x");

        _store.Delete("../tankatlas.db");
        _store.Delete("..\\tankatlas.db");

        Assert.True(File.Exists(outside));
        Assert.Null(_store.GetFullPath("../tankatlas.db"));
        Assert.False(_store.Exists("beleg.jpg"));
        Assert.False(ReceiptPhotoStore.IsValidFileName(Guid.NewGuid().ToString("N") + ".exe"));
    }

    /// <summary>
    /// Prüft, dass die Bereinigung nur nicht referenzierte Fotos der Ablage entfernt.
    /// </summary>
    [Fact]
    public async Task DeleteUnreferenced_KeepsReferencedFiles()
    {
        var kept = (await _store.ImportAsync(new MemoryStream([1]), ".jpg")).FileName!;
        var orphan = (await _store.ImportAsync(new MemoryStream([2]), ".jpg")).FileName!;
        var foreign = Path.Combine(ReceiptDirectory, "notiz.txt");
        File.WriteAllText(foreign, "x");

        _store.DeleteUnreferenced(new HashSet<string>([kept]));

        Assert.True(_store.Exists(kept));
        Assert.False(_store.Exists(orphan));
        Assert.True(File.Exists(foreign));
    }
}
