using System.IO.Compression;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Grenzen gegen Zip-Bomben des <see cref="BackupArchiveValidator"/>.
/// </summary>
public class BackupArchiveValidatorTests_Limits : BaseTest
{
    private static readonly IReadOnlyList<string> Known = [BackupTestArchive.DefaultMigration];

    /// <summary>
    /// Prüft die Konstanten der Grenzen.
    /// </summary>
    [Fact]
    public void Limits_HaveDocumentedValues()
    {
        Assert.Equal(20_000, BackupLimits.MaxEntries);
        Assert.Equal(2L * 1024 * 1024 * 1024, BackupLimits.MaxTotalBytes);
        Assert.Equal(1L * 1024 * 1024 * 1024, BackupLimits.MaxDatabaseBytes);
        Assert.Equal(1024 * 1024, BackupLimits.MaxManifestBytes);
    }

    /// <summary>
    /// Prüft, dass ein Archiv mit mehr Einträgen als erlaubt abgelehnt wird.
    /// </summary>
    [Fact]
    public void Validate_TooManyEntries_IsInvalid()
    {
        using var temp = new TempDirectory();
        var path = temp.Combine("many.zip");
        BackupTestArchive.Create(path, [1]);
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
        {
            for (var index = 0; index <= BackupLimits.MaxEntries; index++)
            {
                archive.CreateEntry("x/" + index);
            }
        }

        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(path, Known).Result);
    }

    /// <summary>
    /// Prüft, dass ein Foto über der Größengrenze abgelehnt wird, auch wenn es gut komprimiert ist.
    /// </summary>
    [Fact]
    public void Validate_PhotoOverLimit_IsInvalid()
    {
        using var temp = new TempDirectory();
        var path = temp.Combine("big.zip");
        var photo = new byte[(int)TankbookEntryLimits.MaxReceiptPhotoBytes + 1];
        BackupTestArchive.Create(path, [1], new Dictionary<string, byte[]> { ["0123456789abcdef0123456789abcdef.jpg"] = photo });

        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(path, Known).Result);
    }

    /// <summary>
    /// Prüft, dass ein Manifest über der Größengrenze abgelehnt wird.
    /// </summary>
    [Fact]
    public void Validate_ManifestOverLimit_IsInvalid()
    {
        using var temp = new TempDirectory();
        var path = temp.Combine("manifest.zip");
        var padding = new string(' ', (int)BackupLimits.MaxManifestBytes + 10);
        BackupTestArchive.CreateWithManifestBytes(path, [1], System.Text.Encoding.UTF8.GetBytes("{" + padding + "}"));

        Assert.Equal(BackupResult.InvalidFile, BackupArchiveValidator.Validate(path, Known).Result);
    }
}
