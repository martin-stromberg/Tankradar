using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Whitelist der Eintragsnamen und den Zip-Slip-Schutz des <see cref="BackupArchiveValidator"/>.
/// </summary>
public class BackupArchiveValidatorTests_EntryNames : BaseTest
{
    private static readonly IReadOnlyList<string> Known = [BackupTestArchive.DefaultMigration];
    private static readonly byte[] Database = [1, 2, 3, 4];

    /// <summary>
    /// Prüft, dass die drei erlaubten Arten von Namen akzeptiert werden.
    /// </summary>
    /// <param name="name">Der Eintragsname.</param>
    [Theory]
    [InlineData("manifest.json")]
    [InlineData("database/tankatlas.db")]
    [InlineData("receipts/0123456789abcdef0123456789abcdef.jpg")]
    [InlineData("receipts/0123456789ABCDEF0123456789ABCDEF.png")]
    public void IsValidEntryName_Allowed(string name)
    {
        Assert.True(BackupArchiveValidator.IsValidEntryName(name));
    }

    /// <summary>
    /// Prüft, dass Pfadtricks, absolute Pfade, Laufwerke, Verzeichniseinträge, unbekannte Einträge und ungültige Fotonamen abgelehnt werden.
    /// </summary>
    /// <param name="name">Der Eintragsname.</param>
    [Theory]
    [InlineData("")]
    [InlineData("../x")]
    [InlineData("..\\x")]
    [InlineData("/abs")]
    [InlineData("C:\\x")]
    [InlineData("C:/x")]
    [InlineData("receipts/../database/tankatlas.db")]
    [InlineData("receipts/")]
    [InlineData("receipts")]
    [InlineData("database/")]
    [InlineData("database\\tankatlas.db")]
    [InlineData("Manifest.json")]
    [InlineData("tiles/1.png")]
    [InlineData("secrets.txt")]
    [InlineData("receipts/foto.jpg")]
    [InlineData("receipts/0123456789abcdef0123456789abcdef.exe")]
    [InlineData("receipts/sub/0123456789abcdef0123456789abcdef.jpg")]
    public void IsValidEntryName_Rejected(string name)
    {
        Assert.False(BackupArchiveValidator.IsValidEntryName(name));
    }

    /// <summary>
    /// Prüft, dass <see langword="null"/> kein gültiger Name ist.
    /// </summary>
    [Fact]
    public void IsValidEntryName_Null_Rejected()
    {
        Assert.False(BackupArchiveValidator.IsValidEntryName(null));
    }

    /// <summary>
    /// Prüft, dass Duplikate ohne Beachtung der Groß- und Kleinschreibung und das Fehlen von Manifest oder Datenbank abgelehnt werden.
    /// </summary>
    [Fact]
    public void AreEntryNamesValid_DuplicatesAndMissingParts()
    {
        const string Photo = "receipts/0123456789abcdef0123456789abcdef.jpg";
        Assert.True(BackupArchiveValidator.AreEntryNamesValid(["manifest.json", "database/tankatlas.db", Photo]));
        Assert.False(BackupArchiveValidator.AreEntryNamesValid(["manifest.json", "database/tankatlas.db", Photo, Photo.ToUpperInvariant().Replace("RECEIPTS/", "receipts/", StringComparison.Ordinal)]));
        Assert.False(BackupArchiveValidator.AreEntryNamesValid(["manifest.json", "database/tankatlas.db", "manifest.json"]));
        Assert.False(BackupArchiveValidator.AreEntryNamesValid(["manifest.json"]));
        Assert.False(BackupArchiveValidator.AreEntryNamesValid(["database/tankatlas.db"]));
    }

    /// <summary>
    /// Prüft, dass ein Archiv mit Zip-Slip-Eintrag auch dann abgelehnt wird, wenn der Eintrag im Manifest steht.
    /// </summary>
    /// <param name="name">Der Name des zusätzlichen Eintrags.</param>
    [Theory]
    [InlineData("../evil.txt")]
    [InlineData("..\\evil.txt")]
    [InlineData("/evil.txt")]
    [InlineData("C:\\evil.txt")]
    [InlineData("receipts/../database/tankatlas.db")]
    [InlineData("tiles/1.png")]
    public void Validate_ArchiveWithBadEntry_IsInvalid(string name)
    {
        using var temp = new TempDirectory();
        var path = temp.Combine("a.zip");
        BackupTestArchive.Create(path, Database, extraEntries: [(name, [9])], listExtraEntriesInManifest: true);

        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(path, Known).Result);
    }

    /// <summary>
    /// Prüft, dass ein Verzeichniseintrag und ein ungültiger Fotoname abgelehnt werden.
    /// </summary>
    [Fact]
    public void Validate_DirectoryEntryAndBadPhotoName_AreInvalid()
    {
        using var temp = new TempDirectory();
        var directory = temp.Combine("dir.zip");
        BackupTestArchive.Create(directory, Database, extraEntries: [("receipts/", [])], listExtraEntriesInManifest: true);
        var photo = temp.Combine("photo.zip");
        BackupTestArchive.Create(photo, Database, photos: new Dictionary<string, byte[]> { ["foto.jpg"] = [1] });

        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(directory, Known).Result);
        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(photo, Known).Result);
    }

    /// <summary>
    /// Prüft, dass ein gültiges Archiv mit Foto akzeptiert wird.
    /// </summary>
    [Fact]
    public void Validate_ValidArchive_IsOk()
    {
        using var temp = new TempDirectory();
        var path = temp.Combine("a.zip");
        BackupTestArchive.Create(path, Database, new Dictionary<string, byte[]> { ["0123456789abcdef0123456789abcdef.jpg"] = [0xFF, 0xD8, 0xFF] });

        var outcome = BackupArchiveValidator.Validate(path, Known);

        Assert.Equal(BackupResult.Ok, outcome.Result);
        Assert.Equal(2, outcome.Manifest!.Files.Count);
    }

    /// <summary>
    /// Prüft die Dateinamen-Regel für Sicherungen: nur das Muster <c>tankatlas-backup-*.zip</c> ohne Pfadanteile.
    /// </summary>
    /// <param name="name">Der Dateiname.</param>
    /// <param name="expected">Das erwartete Ergebnis.</param>
    [Theory]
    [InlineData("tankatlas-backup-20261012-131500.zip", true)]
    [InlineData("tankatlas-backup-20261012-131500-2.zip", true)]
    [InlineData("..\\tankatlas-backup-1.zip", false)]
    [InlineData("sub/tankatlas-backup-1.zip", false)]
    [InlineData("C:\\tankatlas-backup-1.zip", false)]
    [InlineData("tankatlas-backup-1.txt", false)]
    [InlineData("other.zip", false)]
    [InlineData("", false)]
    public void BackupFormat_IsValidFileName(string name, bool expected)
    {
        Assert.Equal(expected, BackupFormat.IsValidFileName(name));
    }
}
