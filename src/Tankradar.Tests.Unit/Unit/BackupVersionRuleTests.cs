using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Versionsregeln: ältere Datenbankstände sind erlaubt, neuere werden als „zu neu“ abgelehnt, unbekannte sind ungültig.
/// </summary>
public class BackupVersionRuleTests : BaseTest
{
    private static readonly IReadOnlyList<string> Known = ["20261004180805_InitialCreate", "20261006080459_AddFavorites", "20261011170920_AddTankbookEntryDetails"];

    private static BackupManifest Manifest(string migration, int schemaVersion = 1)
    {
        return new BackupManifest { DatabaseMigration = migration, SchemaVersion = schemaVersion };
    }

    /// <summary>
    /// Prüft, dass die aktuelle und eine ältere bekannte Migration erlaubt sind.
    /// </summary>
    /// <param name="migration">Der Testwert migration.</param>
    [Theory]
    [InlineData("20261011170920_AddTankbookEntryDetails")]
    [InlineData("20261006080459_AddFavorites")]
    [InlineData("20261004180805_InitialCreate")]
    public void CheckVersion_KnownMigration_IsOk(string migration)
    {
        Assert.Equal(BackupResult.Ok, BackupArchiveValidator.CheckVersion(Manifest(migration), Known));
    }

    /// <summary>
    /// Prüft, dass eine neuere Migration als zu neu gilt.
    /// </summary>
    [Fact]
    public void CheckVersion_NewerMigration_IsTooNew()
    {
        Assert.Equal(BackupResult.TooNew, BackupArchiveValidator.CheckVersion(Manifest("20271231000000_AddSomethingNew"), Known));
    }

    /// <summary>
    /// Prüft, dass eine unbekannte, aber ältere oder leere Migration ungültig ist.
    /// </summary>
    /// <param name="migration">Der Testwert migration.</param>
    [Theory]
    [InlineData("20261005000000_Unknown")]
    [InlineData("")]
    [InlineData("   ")]
    public void CheckVersion_UnknownOrEmptyMigration_IsInvalid(string migration)
    {
        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.CheckVersion(Manifest(migration), Known));
    }

    /// <summary>
    /// Prüft die Schemaversion des Formats: 0 ist ungültig, eine neuere Version ist zu neu, die aktuelle ist erlaubt.
    /// </summary>
    [Fact]
    public void CheckVersion_SchemaVersion()
    {
        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.CheckVersion(Manifest(Known[2], 0), Known));
        Assert.Equal(BackupResult.TooNew, BackupArchiveValidator.CheckVersion(Manifest(Known[2], BackupFormat.SchemaVersion + 1), Known));
        Assert.Equal(BackupResult.Ok, BackupArchiveValidator.CheckVersion(Manifest(Known[2], BackupFormat.SchemaVersion), Known));
    }

    /// <summary>
    /// Prüft, dass eine falsche Formatkennung ungültig ist.
    /// </summary>
    [Fact]
    public void CheckVersion_WrongFormat_IsInvalid()
    {
        var manifest = Manifest(Known[2]);
        manifest.Format = "Fremd";

        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.CheckVersion(manifest, Known));
    }
}
