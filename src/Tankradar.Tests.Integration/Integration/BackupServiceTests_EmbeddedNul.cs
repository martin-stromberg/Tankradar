using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft (H2), dass eine Sicherung mit eingebettetem NUL-Zeichen in Textfeldern (<c>'a' || char(0) || 200.000 Zeichen</c>) die Längenprüfung nicht umgeht: SQLite zählt Text
/// bis zum ersten NUL, die zusätzliche Byteprüfung (<c>length(CAST(col AS BLOB))</c>, ADR 0011) erkennt den Überhang. Erwartet wird die Ablehnung mit
/// <see cref="BackupResult.InvalidFile"/> und unveränderter Bestand; zulässig wäre auch die Annahme mit gekürztem Wert, aber nie ein Absturz oder ein überlanger Wert.
/// </summary>
[Collection(BackupCollection.Name)]
public class BackupServiceTests_EmbeddedNul : IDisposable
{
    private const string Payload = "'a' || char(0) || replace(hex(zeroblob(100000)), '0', 'x')";
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly BackupTestEnvironment _environment = new();

    /// <summary>
    /// Die Felder, deren Inhalt manipuliert wird, als SQL-Anweisung.
    /// </summary>
    public static TheoryData<string, string> Fields
    {
        get
        {
            return new TheoryData<string, string>
            {
                { "Stations.CustomName", "UPDATE Stations SET CustomName = " + Payload },
                { "Stations.Name", "UPDATE Stations SET Name = " + Payload },
                { "Stations.Brand", "UPDATE Stations SET Brand = " + Payload },
                { "TankbookEntries.Note", "UPDATE TankbookEntries SET Note = " + Payload },
            };
        }
    }

    private async Task<string> CreateTamperedArchiveAsync(string sql)
    {
        await _environment.SeedAsync(Photo);
        var created = await _environment.CreateService().CreateAsync();
        var source = Path.Combine(_environment.BackupDirectory, created.File!.FileName);
        var target = Path.Combine(_environment.BackupDirectory, "tankatlas-backup-20261001-100000.zip");
        BackupTestTampering.CreateTamperedCopy(source, target, sql);
        File.Delete(source);
        return target;
    }

    /// <summary>
    /// Prüft die Wiederherstellung: Ablehnung mit unverändertem Bestand oder Annahme mit höchstens der Grenzlänge; nie ein Absturz.
    /// </summary>
    /// <param name="field">Das manipulierte Feld (Beschreibung).</param>
    /// <param name="sql">Die manipulierende Anweisung.</param>
    [Theory]
    [MemberData(nameof(Fields))]
    public async Task Restore_EmbeddedNul_IsRejectedOrTruncated_AndKeepsOtherData(string field, string sql)
    {
        var archive = await CreateTamperedArchiveAsync(sql);
        var before = _environment.Snapshot();

        var result = await _environment.CreateService().RestoreAsync(Path.GetFileName(archive)).WaitAsync(TimeSpan.FromSeconds(60));

        if (result == BackupResult.Ok)
        {
            await AssertAcceptedValuesAreLimitedAsync(field);
            return;
        }

        Assert.Equal(BackupResult.InvalidFile, result);
        Assert.Equal(before, _environment.Snapshot());
        Assert.Equal(["Golf"], await _environment.GetVehicleNamesAsync());
        Assert.False(Directory.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.StagingDirectoryName)));
        Assert.False(RestoreJournal.Exists(_environment.DataDirectory));
        Assert.Equal(0, _environment.CacheInvalidations);
    }

    /// <summary>
    /// Prüft, dass schon das Einspielen der Datei nicht abstürzt: Entweder wird sie ohne Rest abgelehnt, oder sie gelangt unverändert in die Liste.
    /// </summary>
    /// <param name="field">Das manipulierte Feld (Beschreibung).</param>
    /// <param name="sql">Die manipulierende Anweisung.</param>
    [Theory]
    [MemberData(nameof(Fields))]
    public async Task Import_EmbeddedNul_IsRejectedOrListed_WithoutLeftovers(string field, string sql)
    {
        Assert.NotEmpty(field);
        var archive = await CreateTamperedArchiveAsync(sql);
        var external = Path.Combine(_environment.DataDirectory, "extern.zip");
        File.Move(archive, external);
        var before = _environment.Snapshot();
        var service = _environment.CreateService();
        var picked = new PickedBackupFile(Path.GetFileName(external), () => Task.FromResult<Stream>(File.OpenRead(external)));

        var outcome = await service.ImportAsync(picked).WaitAsync(TimeSpan.FromSeconds(60));

        if (outcome.Result == BackupResult.Ok)
        {
            Assert.Single(await service.ListAsync());
            Assert.Equal(before, _environment.Snapshot());
            return;
        }

        Assert.Equal(BackupResult.InvalidFile, outcome.Result);
        Assert.Empty(await service.ListAsync());
        Assert.Empty(Directory.GetFileSystemEntries(_environment.BackupDirectory));
        Assert.Equal(before, _environment.Snapshot());
    }

    private async Task AssertAcceptedValuesAreLimitedAsync(string field)
    {
        await using var context = _environment.CreateFactory().CreateDbContext();
        var station = await context.Stations.SingleAsync();
        var entry = await context.TankbookEntries.SingleAsync();
        Assert.True((station.CustomName?.Length ?? 0) <= 60, field);
        Assert.True(station.Name.Length <= 200, field);
        Assert.True((station.Brand?.Length ?? 0) <= 200, field);
        Assert.True((entry.Note?.Length ?? 0) <= 2000, field);
        var stored = field switch
        {
            "Stations.CustomName" => station.CustomName,
            "Stations.Name" => station.Name,
            "Stations.Brand" => station.Brand,
            _ => entry.Note,
        };
        Assert.Equal("a", stored);
        Assert.Equal(["Golf"], await _environment.GetVehicleNamesAsync());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
        GC.SuppressFinalize(this);
    }
}
