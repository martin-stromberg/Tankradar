using System.Text;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Manifest, Dateiliste, Prüfsummen und Entpacken des <see cref="BackupArchiveValidator"/>.
/// </summary>
public class BackupArchiveValidatorTests_Manifest : BaseTest
{
    private const string PhotoName = "0123456789abcdef0123456789abcdef.jpg";
    private static readonly IReadOnlyList<string> Known = [BackupTestArchive.DefaultMigration];
    private static readonly byte[] Database = Encoding.ASCII.GetBytes("database-content");
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];

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
    /// Prüft, dass ein Archiv ohne Manifest, mit leerem Manifest und mit Text statt JSON als ungültig gilt.
    /// </summary>
    [Fact]
    public void Validate_MissingEmptyOrNoJsonManifest_IsInvalid()
    {
        var missing = _temp.Combine("missing.zip");
        BackupTestArchive.CreateWithoutManifest(missing, Database);
        var empty = _temp.Combine("empty.zip");
        BackupTestArchive.CreateWithManifestBytes(empty, Database, []);
        var text = _temp.Combine("text.zip");
        BackupTestArchive.CreateWithManifestBytes(text, Database, Encoding.UTF8.GetBytes("das ist kein json"));
        var nulls = _temp.Combine("null.zip");
        BackupTestArchive.CreateWithManifestBytes(nulls, Database, Encoding.UTF8.GetBytes("{\"format\":\"Tankatlas-Backup\",\"schemaVersion\":1,\"databaseMigration\":\"" + BackupTestArchive.DefaultMigration + "\",\"files\":null,\"counts\":null}"));

        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(missing, Known).Result);
        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(empty, Known).Result);
        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(text, Known).Result);
        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(nulls, Known).Result);
    }

    /// <summary>
    /// Prüft, dass eine leere, eine abgeschnittene und eine Nicht-ZIP-Datei als ungültig gelten.
    /// </summary>
    [Fact]
    public void Validate_EmptyTruncatedOrNotAZip_IsInvalid()
    {
        var empty = _temp.Combine("empty.zip");
        File.WriteAllBytes(empty, []);
        var notZip = _temp.Combine("text.zip");
        File.WriteAllText(notZip, "Hallo");
        var truncated = _temp.Combine("truncated.zip");
        BackupTestArchive.Create(truncated, Database);
        BackupTestArchive.Truncate(truncated);

        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(empty, Known).Result);
        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(notZip, Known).Result);
        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(truncated, Known).Result);
    }

    /// <summary>
    /// Prüft Formatkennung und Schemaversion: falsches Format und Version 0 sind ungültig, Version 99 ist zu neu.
    /// </summary>
    [Fact]
    public void Validate_FormatAndSchemaVersion()
    {
        var wrongFormat = _temp.Combine("format.zip");
        BackupTestArchive.Create(wrongFormat, Database, format: "Anderes");
        var zero = _temp.Combine("zero.zip");
        BackupTestArchive.Create(zero, Database, schemaVersion: 0);
        var future = _temp.Combine("future.zip");
        BackupTestArchive.Create(future, Database, schemaVersion: 99, appVersion: "9.9.9");

        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(wrongFormat, Known).Result);
        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(zero, Known).Result);
        var tooNew = BackupArchiveValidator.Validate(future, Known);
        Assert.Equal(BackupResult.TooNew, tooNew.Result);
        Assert.Equal("9.9.9", tooNew.Manifest!.AppVersion);
    }

    /// <summary>
    /// Prüft, dass eine Dateiliste, die vom Archivinhalt abweicht, und eine falsch deklarierte Größe als ungültig gelten.
    /// </summary>
    [Fact]
    public void Validate_FileListDeviatesOrWrongSize_IsInvalid()
    {
        var extra = _temp.Combine("extra.zip");
        BackupTestArchive.Create(extra, Database, new Dictionary<string, byte[]> { [PhotoName] = Jpeg });
        using (var archive = System.IO.Compression.ZipFile.Open(extra, System.IO.Compression.ZipArchiveMode.Update))
        {
            using var stream = archive.CreateEntry("receipts/fedcba9876543210fedcba9876543210.png").Open();
            stream.Write([1, 2, 3]);
        }

        var wrongSize = _temp.Combine("size.zip");
        BackupTestArchive.Create(wrongSize, Database, declaredDatabaseSize: Database.Length - 1);

        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(extra, Known).Result);
        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(wrongSize, Known).Result);
    }

    /// <summary>
    /// Prüft, dass eine falsche Prüfsumme erst beim Lesen der Inhalte auffällt und das Entpacken sie ebenfalls ablehnt.
    /// </summary>
    [Fact]
    public async Task WrongHash_IsDetectedByVerifyAndExtract()
    {
        var path = _temp.Combine("hash.zip");
        BackupTestArchive.Create(path, Database, wrongDatabaseHash: true);
        var manifest = BackupArchiveValidator.Validate(path, Known).Manifest!;

        Assert.Equal(BackupResult.InvalidFile, await BackupArchiveValidator.VerifyContentAsync(path, manifest));
        Assert.Equal(BackupResult.InvalidFile, await BackupArchiveValidator.ExtractAsync(path, manifest, _temp.Combine("staging")));
    }

    /// <summary>
    /// Prüft, dass ein gültiges Archiv die Inhalte prüft und in das Staging entpackt (Datenbank und Foto an festen Zielen).
    /// </summary>
    [Fact]
    public async Task ValidArchive_VerifiesAndExtracts()
    {
        var path = _temp.Combine("ok.zip");
        BackupTestArchive.Create(path, Database, new Dictionary<string, byte[]> { [PhotoName] = Jpeg });
        var manifest = BackupArchiveValidator.Validate(path, Known).Manifest!;
        var staging = _temp.Combine("staging");

        Assert.Equal(BackupResult.Ok, await BackupArchiveValidator.VerifyContentAsync(path, manifest));
        Assert.Equal(BackupResult.Ok, await BackupArchiveValidator.ExtractAsync(path, manifest, staging));

        Assert.Equal(Database, await File.ReadAllBytesAsync(Path.Combine(staging, "tankatlas.db")));
        Assert.Equal(Jpeg, await File.ReadAllBytesAsync(Path.Combine(staging, "receipts", PhotoName)));
        Assert.Equal(["receipts", "tankatlas.db"], Directory.EnumerateFileSystemEntries(staging).Select(Path.GetFileName).Order());
    }

    /// <summary>
    /// Prüft, dass ein Foto, dessen Inhalt nicht zur Endung passt, beim Entpacken abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task Extract_PhotoWithWrongSignature_IsInvalid()
    {
        var path = _temp.Combine("photo.zip");
        BackupTestArchive.Create(path, Database, new Dictionary<string, byte[]> { [PhotoName] = [1, 2, 3, 4] });
        var manifest = BackupArchiveValidator.Validate(path, Known).Manifest!;

        Assert.Equal(BackupResult.InvalidFile, await BackupArchiveValidator.ExtractAsync(path, manifest, _temp.Combine("staging")));
    }

    /// <summary>
    /// Prüft, dass das Lesen mehr Bytes als deklariert (Zip-Bomb) als ungültig gilt.
    /// </summary>
    [Fact]
    public async Task Verify_MoreBytesThanDeclared_IsInvalid()
    {
        var path = _temp.Combine("bomb.zip");
        BackupTestArchive.Create(path, Database);
        var manifest = BackupArchiveValidator.Validate(path, Known).Manifest!;
        manifest.Files[0].Size = Database.Length - 1;

        Assert.Equal(BackupResult.InvalidFile, await BackupArchiveValidator.VerifyContentAsync(path, manifest));
    }

    /// <summary>
    /// Prüft, dass <see cref="BackupArchiveValidator.ReadManifest(string)"/> bei nicht lesbaren Dateien <see langword="null"/> liefert.
    /// </summary>
    [Fact]
    public void ReadManifest_Unreadable_ReturnsNull()
    {
        var path = _temp.Combine("text.zip");
        File.WriteAllText(path, "kein zip");

        Assert.Null(BackupArchiveValidator.ReadManifest(path));
        Assert.Null(BackupArchiveValidator.ReadManifest(_temp.Combine("fehlt.zip")));
    }
}
