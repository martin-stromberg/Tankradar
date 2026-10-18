using Tankradar.MAUI.Models.Backup;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass ein Fehler beim Lesen der Sicherungsdatenbank (abgeschnittene oder beschädigte Datei) als ungültige Datei gemeldet wird und nicht als
/// vorübergehender Fehler der Umgebung, den ein erneuter Versuch beheben könnte; die Livedaten bleiben dabei unverändert.
/// </summary>
public class BackupServiceTests_SourceFile : IDisposable
{
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly BackupTestEnvironment _environment = new();

    /// <summary>
    /// Prüft Datenbanken, die mitten in einer Seite oder nach wenigen Seiten enden: <see cref="BackupResult.InvalidFile"/> statt <see cref="BackupResult.Failed"/>.
    /// </summary>
    /// <param name="keepBytes">Die Zahl der erhaltenen Bytes am Anfang der Datenbank.</param>
    [Theory]
    [InlineData(100)]
    [InlineData(4096 + 100)]
    [InlineData(4096 * 3)]
    public async Task Restore_TruncatedDatabase_IsInvalidFile_AndKeepsLiveData(int keepBytes)
    {
        await _environment.SeedAsync(Photo);
        var full = await File.ReadAllBytesAsync(Path.Combine(_environment.DataDirectory, "tankatlas.db"));
        Directory.CreateDirectory(_environment.BackupDirectory);
        const string name = "tankatlas-backup-20261001-100000.zip";
        BackupTestArchive.Create(Path.Combine(_environment.BackupDirectory, name), full.Take(Math.Min(keepBytes, full.Length)).ToArray());
        var before = _environment.Snapshot();

        var result = await _environment.CreateService().RestoreAsync(name);

        Assert.Equal(BackupResult.InvalidFile, result);
        Assert.Equal(before, _environment.Snapshot());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
        GC.SuppressFinalize(this);
    }
}
